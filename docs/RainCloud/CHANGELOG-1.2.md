# WeatherWatcher 1.2.0 development — 2026-09-18

## Changes

- Removed Arduino MLX90614/I2C acquisition, calibration and IR telemetry. Retained RG-11 power/contact checks, USB 9600-baud JSON v1, counters, commands and two-second heartbeat; added immediate state-change reports.
- Removed IR/cloud safety gating. Windows now enforces the persisted configurable RG-11 dry-out delay, default 300 seconds. Wet/fault states and serial expiry remain unsafe. Existing independent weather checks and SafetyMonitor record format remain unchanged.
- Added shared asynchronous NOAA GOES-19 ABI-L2-ACMF retrieval and geographic mask sampling. Defaults: 15 km radius, 600-second polling, 1800-second stale timeout. Missing/stale cloud data is unavailable through ASCOM and never vetoes safety.
- Added coordinates and timing/radius settings, rain/NOAA/safety transition logs, dependency packaging and current installation documentation.
- Preserved the executable's strong name. PureHDF receives a strong name in the build output through build-only StrongNamer; the original NuGet package is unchanged.

## Validation

- Both complete ASCOM solutions built in Release; WeatherWatcher also built in x64 Release. Final builds had no compilation errors or warnings.
- Arduino Uno compiled with Arduino AVR Boards 1.8.8: 3,214 bytes flash (9%), 254 bytes static RAM (12%). No third-party Arduino libraries.
- 166 offline checks passed, covering existing weather functionality plus rain recovery/faults, legacy-frame compatibility, NOAA cache/polling/site changes, unavailable/stale clouds, and safety independence during a blocked download.
- Two real-file checks passed: NOAA NetCDF decoding and decoding through the actual strong-named WeatherWatcher executable.
- Live listing/download/decode passed against the public NOAA bucket. Total with real-file/live checks: **169 passed**.
- The September 18 15:10:20.7 UTC fixture at test coordinates 35°N, 80°W and 15 km radius returned 67 cloudy / 125 valid pixels = **53.6%**. An independent h5py/numpy full-disk calculation matched exactly. These are test coordinates, not an assumed observatory location.
- Setup dialog rendered and inspected. Development installer compiled with all required runtime DLLs and license notices.

No drivers were installed or registered, no firmware was uploaded, no production safety file was written by tests, and no physical rain/dome test was performed. Actual observatory coordinates remain unset. Follow BENCH-TEST.md before unattended deployment.

Development installer: `dist/CCDASTRO.WeatherWatcher-Safety.Setup-1.2.0-development.exe`.
Uno build output: `dist/firmware/RainCloudUno.ino.hex`.
