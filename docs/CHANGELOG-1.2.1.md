# Version 1.2.1: NOAA cloud information and Pushover advisories

This update removes the Arduino IR sensor, keeps local RG-11 rain protection, adds configurable Windows dry-out, and retrieves informational NOAA GOES-East cloud-mask data. Optional Pushover advisories use separate warning/clearing thresholds and a cooldown; delivery failures cannot affect safety.

The combined repository retains SafetyMonitor 1.3.1 and its AnyCPU architecture guard. The canonical build passed 219 offline checks and two additional real-NOAA-file checks, including decoding through the actual signed executable. Both Windows configurations and the combined installer built successfully. No real Pushover messages were sent; installation, physical rain testing and dome qualification remain outstanding.

Updated documentation covers configuration defaults, credentials, caching/freshness, serial compatibility, migration, limitations and commissioning. The README, portable guide, website home-page WeatherWatcher card and weather/safety page now describe the current design. Website upload files are in `website/`; pushing GitHub source does not deploy ccdastro.com.

Build `./build.ps1` to create `dist/CCDASTRO.WeatherWatcher-Safety.Setup-1.2.1-development.exe`. Published downloads remain listed on GitHub Releases. Do not use the historical IR diagrams as current wiring instructions.
