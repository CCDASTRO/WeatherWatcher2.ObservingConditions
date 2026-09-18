# NOAA cloud information

WeatherWatcher reads the public NOAA GOES-East (currently GOES-19) ABI Level-2 Clear Sky Mask, full-disk product `ABI-L2-ACMF`. It lists the current and previous UTC hours at `https://noaa-goes19.s3.amazonaws.com/` and downloads the latest `.nc` object. No account or API key is required. The full disk covers more observatory locations than CONUS; its nominal ten-minute update cadence matches the default poll interval.

The source is a scientific NetCDF4/HDF5 product, not a rendered satellite image. PureHDF 1.0.1 reads the packed fixed-grid coordinates, projection metadata, `BCM` binary mask and `DQF` quality flags. Version 1.x supports the existing .NET Framework 4.7.2 application without native NetCDF DLLs. The installer includes a strong-named build copy of PureHDF and its runtime dependencies. Build-only StrongNamer 0.2.5 performs the compatibility signing required by the existing signed .NET Framework application.

For each valid Earth pixel centered within the configured great-circle radius, BCM 0 means clear/probably clear and BCM 1 means cloudy/probably cloudy. CloudCover is `100 × cloudy good-quality pixels / all good-quality pixels`. Only DQF 0 is accepted. At least three pixels and 80% good-quality coverage are required. Fill, bad, degraded and space pixels never become clear observations. Regions outside the satellite view are unavailable. This is a regional satellite cloud fraction, not a rain detector, forecast, local all-sky camera measurement or cloud-height/parallax correction. The pixel fraction approximates area fraction over the small region; pixels grow toward the disk edge.

Defaults: radius **15 km**, poll **600 seconds**, stale after **1800 seconds**. Limits: radius 5–100 km, polling 600–3600 seconds, stale timeout 600–86400 seconds (at least the polling interval). Latitude/longitude are decimal degrees, north/east positive; western longitudes are negative. Both coordinates may be left blank for rain-only operation. No observatory location is assumed.

The acquisition start timestamp (`time_coverage_start`) is retained conservatively because a disk scan spans roughly ten minutes. ASCOM `TimeSinceLastUpdate("CloudCover")` reports its age, not time since download. No future timestamp is accepted. A shared process cache avoids duplicate client downloads, repeated granule downloads and overlapping requests. Downloads have a 90-second timeout and 64 MiB limit and occur entirely off the serial/ASCOM request paths. Only the local sampling rectangle is decoded from BCM/DQF to limit memory use.

On retrieval failure the last valid result remains available until its acquisition timestamp expires. Stale or absent data produces an ASCOM DriverException for CloudCover and its update age, and NaN in the internal snapshot. The last observation is retained in memory for diagnostics; restarting requires a new fetch. Coordinates/radius changes invalidate the previous site's cache. None of these states enters the SAFE/UNSAFE calculation.

Enable trace logging to record retrieval attempts/failures, observation UTC, age, cloud percentage, pixel counts and transitions to unavailable. If NOAA changes its operational satellite/bucket or product format, update this provider; local RG-11 protection continues independently.

## Primary references

- [NOAA clear sky mask dataset](https://www.ncei.noaa.gov/access/metadata/landing-page/bin/iso?id=gov.noaa.ncdc%3AC01503)
- [NOAA GOES-19 operational transition](https://www.ospo.noaa.gov/data/messages/2025/04/MSG_20250407_1510.html)
- [NOAA full-disk product cadence and quality](https://www.ospo.noaa.gov/operations/goes/product-quality-overview/ps-pvr/goes-16/ABI/Clear%20Sky%20Mask/Full/GOES-16_ABI_L2_Cloud_Mask_Full_README.pdf)
- [NOAA fixed-grid navigation equations](https://www.star.nesdis.noaa.gov/atmospheric-composition-training/satellite_data_goes_imager_projection.php)
- [PureHDF source and license](https://github.com/Apollo3zehn/PureHDF)
