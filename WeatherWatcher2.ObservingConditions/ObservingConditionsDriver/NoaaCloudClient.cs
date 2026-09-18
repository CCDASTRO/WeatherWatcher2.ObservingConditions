using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using ASCOM.Utilities;

namespace WeatherWatcher2.ObservingConditions
{
    internal sealed class CloudReading
    {
        internal double Percent;
        internal DateTime ObservationUtc;
        internal string Source;
        internal int ValidPixels, TotalPixels;
        internal bool IsFresh(DateTime now, int staleSeconds)
        {
            double age = (now - ObservationUtc).TotalSeconds;
            return age >= 0 && age < staleSeconds && Percent >= 0 && Percent <= 100;
        }
    }

    internal sealed class NoaaSite
    {
        internal double Latitude, Longitude, RadiusKm;
        internal string Key => Latitude.ToString("R", CultureInfo.InvariantCulture) + "|" + Longitude.ToString("R", CultureInfo.InvariantCulture) + "|" + RadiusKm.ToString("R", CultureInfo.InvariantCulture);
        internal bool Valid => Latitude >= -90 && Latitude <= 90 && Longitude >= -180 && Longitude <= 180 && RadiusKm >= 5 && RadiusKm <= 100;
        internal static NoaaSite Current => new NoaaSite { Latitude = DriverSettings.ObservatoryLatitude, Longitude = DriverSettings.ObservatoryLongitude, RadiusKm = DriverSettings.NoaaRadiusKm };
    }

    // A shared, memory-only cache: ASCOM calls never wait for the network or NetCDF decoding.
    internal sealed class NoaaCloudClient
    {
        internal static readonly NoaaCloudClient Shared = new NoaaCloudClient();
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(90), MaxResponseContentBufferSize = 64 * 1024 * 1024 };
        private const string Root = "https://noaa-goes19.s3.amazonaws.com/";
        private readonly object gate = new object();
        private readonly Func<NoaaSite, string, Task<CloudReading>> fetch;
        private CloudReading cached;
        private string siteKey, error = "No NOAA observation received";
        private DateTime nextPoll = DateTime.MinValue;
        private bool busy;
        internal bool Busy { get { lock (gate) return busy; } }
        internal NoaaCloudClient(Func<NoaaSite, string, Task<CloudReading>> fetch = null) { this.fetch = fetch ?? Retrieve; }

        internal CloudReading Get(NoaaSite site, DateTime now, int pollSeconds, out string status)
        {
            lock (gate)
            {
                if (siteKey != site.Key) { siteKey = site.Key; cached = null; nextPoll = DateTime.MinValue; error = "Waiting for NOAA observation for this site"; }
                if (!site.Valid) { status = "Configure observatory latitude, longitude and NOAA sampling radius"; return null; }
                if (!busy && now >= nextPoll)
                {
                    busy = true;
                    nextPoll = now.AddSeconds(Math.Max(600, pollSeconds));
                    string key = siteKey, previous = cached == null ? null : cached.Source;
                    Task.Run(async () =>
                    {
                        try
                        {
                            Log("Retrieving GOES-19 ABI-L2-ACMF for " + key);
                            var result = await fetch(site, previous).ConfigureAwait(false);
                            CloudReading toLog = null;
                            lock (gate)
                            {
                                if (siteKey == key)
                                {
                                    if (result != null)
                                    {
                                        if (!result.IsFresh(DateTime.UtcNow, int.MaxValue)) throw new InvalidDataException("Invalid or future NOAA observation");
                                        if (cached == null || result.ObservationUtc >= cached.ObservationUtc) cached = result;
                                    }
                                    error = null;
                                    toLog = cached;
                                }
                            }
                            if (toLog != null) Log(string.Format(CultureInfo.InvariantCulture,
                                "Observation {0:O}, age {1:F0}s, cloud {2:F1}%, good pixels {3}/{4}, {5}", toLog.ObservationUtc,
                                (DateTime.UtcNow - toLog.ObservationUtc).TotalSeconds, toLog.Percent, toLog.ValidPixels, toLog.TotalPixels, toLog.Source));
                        }
                        catch (Exception ex)
                        {
                            lock (gate) { if (siteKey == key) error = "NOAA retrieval failed: " + ex.Message; }
                            Log("Retrieval failed; retaining last valid observation: " + ex.Message);
                        }
                        finally { lock (gate) busy = false; }
                    });
                }
                status = error;
                return cached;
            }
        }

        private static void Log(string message)
        {
            if (!DriverSettings.EnableLogging) return;
            try { using (var log = new TraceLogger("", "WeatherWatcher2.NOAA")) { log.Enabled = true; log.LogMessage("GOES", message); } }
            catch { /* Logging cannot affect weather or rain protection. */ }
        }

        internal static async Task<CloudReading> Retrieve(NoaaSite site, string previous)
        {
            DateTime now = DateTime.UtcNow;
            string latest = null;
            // Include the previous UTC hour for normal publication latency and midnight rollover.
            for (int hour = 0; hour < 2; hour++)
            {
                DateTime at = now.AddHours(-hour);
                string prefix = string.Format(CultureInfo.InvariantCulture, "ABI-L2-ACMF/{0:yyyy}/{1:000}/{0:HH}/", at, at.DayOfYear);
                string xml = await Http.GetStringAsync(Root + "?list-type=2&max-keys=100&prefix=" + Uri.EscapeDataString(prefix)).ConfigureAwait(false);
                var document = XDocument.Parse(xml);
                XNamespace ns = "http://s3.amazonaws.com/doc/2006-03-01/";
                string candidate = document.Descendants(ns + "Key").Select(x => x.Value)
                    .Where(x => x.StartsWith(prefix + "OR_ABI-L2-ACMF-M", StringComparison.Ordinal) && x.Contains("_G19_s") && x.EndsWith(".nc", StringComparison.Ordinal))
                    .OrderByDescending(x => x, StringComparer.Ordinal).FirstOrDefault();
                if (candidate != null) { latest = candidate; break; }
            }
            if (latest == null) throw new InvalidDataException("No GOES-East cloud mask published in the last two hours");
            if (latest == previous) return null; // Do not repeatedly download the same 25 MB granule.
            string path = Path.GetTempFileName();
            try
            {
                // Default buffering keeps HttpClient's timeout active through the complete download.
                using (var response = await Http.GetAsync(Root + latest).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    if (response.Content.Headers.ContentLength > 64 * 1024 * 1024) throw new InvalidDataException("Oversized NOAA product");
                    using (var stream = File.Create(path)) await response.Content.CopyToAsync(stream).ConfigureAwait(false);
                }
                return GoesCloudMask.Read(path, site, latest);
            }
            finally { File.Delete(path); }
        }
    }
}
