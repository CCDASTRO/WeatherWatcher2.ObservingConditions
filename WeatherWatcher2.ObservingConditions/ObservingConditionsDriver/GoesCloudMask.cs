using System;
using System.Globalization;
using System.IO;
using PureHDF;
using PureHDF.Selections;

namespace WeatherWatcher2.ObservingConditions
{
    internal sealed class GoesProjection
    {
        internal double Equatorial, Polar, Height, OriginLongitude;
        private const double Rad = Math.PI / 180;
        internal bool Project(double latitude, double longitude, out double x, out double y)
        {
            double c = Math.Atan(Polar * Polar / (Equatorial * Equatorial) * Math.Tan(latitude * Rad));
            double e2 = 1 - Polar * Polar / (Equatorial * Equatorial);
            double r = Polar / Math.Sqrt(1 - e2 * Math.Cos(c) * Math.Cos(c));
            double dl = (longitude - OriginLongitude) * Rad, h = Height + Equatorial;
            double sx = h - r * Math.Cos(c) * Math.Cos(dl), sy = -r * Math.Cos(c) * Math.Sin(dl), sz = r * Math.Sin(c);
            x = Math.Asin(-sy / Math.Sqrt(sx * sx + sy * sy + sz * sz));
            y = Math.Atan2(sz, sx);
            return h * (h - sx) > sy * sy + (h - sx) * (h - sx) + Equatorial * Equatorial / (Polar * Polar) * sz * sz;
        }
        // NOAA GOES-R PUG / STAR fixed-grid navigation, sweep axis x.
        internal bool Locate(double x, double y, out double latitude, out double longitude)
        {
            double h = Height + Equatorial, ratio = Equatorial * Equatorial / (Polar * Polar);
            double a = Math.Sin(x) * Math.Sin(x) + Math.Cos(x) * Math.Cos(x) * (Math.Cos(y) * Math.Cos(y) + ratio * Math.Sin(y) * Math.Sin(y));
            double b = -2 * h * Math.Cos(x) * Math.Cos(y), c = h * h - Equatorial * Equatorial;
            double discriminant = b * b - 4 * a * c;
            latitude = longitude = double.NaN;
            if (discriminant < 0) return false;
            double r = (-b - Math.Sqrt(discriminant)) / (2 * a);
            double sx = r * Math.Cos(x) * Math.Cos(y), sy = -r * Math.Sin(x), sz = r * Math.Cos(x) * Math.Sin(y);
            latitude = Math.Atan(ratio * sz / Math.Sqrt((h - sx) * (h - sx) + sy * sy)) / Rad;
            longitude = OriginLongitude - Math.Atan2(sy, h - sx) / Rad;
            return true;
        }
        internal static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            double a = Math.Pow(Math.Sin((lat2 - lat1) * Rad / 2), 2) + Math.Cos(lat1 * Rad) * Math.Cos(lat2 * Rad) * Math.Pow(Math.Sin((lon2 - lon1) * Rad / 2), 2);
            return 6371.0088 * 2 * Math.Asin(Math.Sqrt(Math.Min(1, a)));
        }
    }

    internal static class GoesCloudMask
    {
        internal static CloudReading Read(string path, NoaaSite site, string source)
        {
            if (!site.Valid) throw new InvalidDataException("Invalid NOAA site configuration");
            using (var file = H5File.OpenRead(path))
            {
                var projection = file.Dataset("goes_imager_projection");
                if (projection.Attribute("sweep_angle_axis").Read<string>() != "x") throw new InvalidDataException("Unsupported GOES sweep axis");
                var geo = new GoesProjection {
                    Equatorial = projection.Attribute("semi_major_axis").Read<double[]>()[0],
                    Polar = projection.Attribute("semi_minor_axis").Read<double[]>()[0],
                    Height = projection.Attribute("perspective_point_height").Read<double[]>()[0],
                    OriginLongitude = projection.Attribute("longitude_of_projection_origin").Read<double[]>()[0]
                };
                double cx, cy;
                if (!geo.Project(site.Latitude, site.Longitude, out cx, out cy)) throw new InvalidDataException("Observatory is outside GOES-East coverage");
                // Reject a region crossing the limb instead of silently measuring only its visible part.
                double latitudeRadians = site.Latitude * Math.PI / 180, angularRadius = site.RadiusKm / 6371.0088;
                for (int bearing = 0; bearing < 360; bearing += 5)
                {
                    double angle = bearing * Math.PI / 180;
                    double edgeLat = Math.Asin(Math.Sin(latitudeRadians) * Math.Cos(angularRadius) + Math.Cos(latitudeRadians) * Math.Sin(angularRadius) * Math.Cos(angle));
                    double edgeLon = site.Longitude + Math.Atan2(Math.Sin(angle) * Math.Sin(angularRadius) * Math.Cos(latitudeRadians),
                        Math.Cos(angularRadius) - Math.Sin(latitudeRadians) * Math.Sin(edgeLat)) * 180 / Math.PI;
                    double edgeX, edgeY;
                    if (!geo.Project(edgeLat * 180 / Math.PI, edgeLon, out edgeX, out edgeY)) throw new InvalidDataException("Sampling circle crosses GOES-East coverage boundary");
                }
                var x = Coordinates(file.Dataset("x")); var y = Coordinates(file.Dataset("y"));
                // Angular radius bound exceeds r / minimum satellite-to-surface distance;
                // exact geodesic distance below selects a circle, not this enclosing rectangle.
                double margin = site.RadiusKm * 1000 / geo.Height * 1.1;
                int left, right, top, bottom;
                Bounds(x, cx - margin, cx + margin, out left, out right);
                Bounds(y, cy - margin, cy + margin, out top, out bottom);
                var mask = file.Dataset("BCM"); var quality = file.Dataset("DQF");
                foreach (var data in new[] { mask, quality })
                    if (data.Space.Dimensions.Length != 2 || data.Space.Dimensions[0] != (ulong)y.Length || data.Space.Dimensions[1] != (ulong)x.Length)
                        throw new InvalidDataException("Unexpected cloud mask grid dimensions");
                var selection = new HyperslabSelection(2, new[] { (ulong)top, (ulong)left }, new[] { (ulong)(bottom - top + 1), (ulong)(right - left + 1) });
                byte[] clouds = mask.Read<byte[]>(fileSelection: selection), flags = quality.Read<byte[]>(fileSelection: selection);
                int total = 0, good = 0, cloudy = 0, index = 0;
                for (int row = top; row <= bottom; row++)
                    for (int col = left; col <= right; col++, index++)
                    {
                        double lat, lon;
                        if (!geo.Locate(x[col], y[row], out lat, out lon) || GoesProjection.DistanceKm(site.Latitude, site.Longitude, lat, lon) > site.RadiusKm) continue;
                        total++;
                        // BCM 0 = clear/probably clear, 1 = cloudy/probably cloudy.
                        // Never count fill, space, bad or degraded pixels as clear.
                        if (flags[index] != 0 || clouds[index] > 1) continue;
                        good++; cloudy += clouds[index];
                    }
                double percent = Percentage(cloudy, good, total);
                // Use start of acquisition conservatively; the disk takes about ten minutes to scan.
                DateTime observation = DateTime.Parse(file.Attribute("time_coverage_start").Read<string>(), CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                return new CloudReading { Percent = percent, ObservationUtc = observation, Source = source, ValidPixels = good, TotalPixels = total };
            }
        }
        internal static double Percentage(int cloudy, int good, int total)
        {
            if (total < 3 || good < total * .8 || cloudy < 0 || cloudy > good) throw new InvalidDataException("Insufficient good-quality NOAA pixels in the sampling radius (requires 80%)");
            return 100.0 * cloudy / good;
        }
        private static double[] Coordinates(IH5Dataset data)
        {
            short[] packed = data.Read<short[]>();
            double scale = data.Attribute("scale_factor").Read<float[]>()[0], offset = data.Attribute("add_offset").Read<float[]>()[0];
            var result = new double[packed.Length];
            for (int i = 0; i < packed.Length; i++) result[i] = packed[i] * scale + offset;
            return result;
        }
        private static void Bounds(double[] axis, double minimum, double maximum, out int first, out int last)
        {
            first = -1; last = -1;
            for (int i = 0; i < axis.Length; i++) if (axis[i] >= minimum && axis[i] <= maximum) { if (first < 0) first = i; last = i; }
            if (first < 1 || last >= axis.Length - 1) throw new InvalidDataException("Sampling region is outside the GOES grid");
            first--; last++;
        }
    }
}
