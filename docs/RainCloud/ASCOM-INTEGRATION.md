# ASCOM integration — development build

WeatherWatcher ObservingConditions 1.2.1 and the existing WeatherWatcher SafetyMonitor 1.3.1 preserve the local-server and `WW2RC1` safety-file architecture. The SafetyMonitor implementation and protocol do not need modification for this change.

## Data and safety paths

- Arduino RG-11 + power/contact monitoring → shared USB reader → rain safety with configurable dry-out → combined existing weather safety → timestamped safety file → SafetyMonitor → existing dome automation.
- NOAA GOES-East ABI Level-2 cloud mask → background Windows retrieval/decoding → shared cache → ASCOM CloudCover. There is no NOAA-to-safety connection.

Select **Use Arduino RG-11 rain protection and NOAA cloud information** in setup. It replaces the former Arduino IR/cloud mode and, as before, takes precedence over Boltwood. Existing Ambient/Cumulus inputs and independent wind, humidity, temperature and selected-source health checks remain. NOAA failure does not create a new safety veto; an independently selected Ambient API source still retains its existing failure behavior.

RG-11 wet/rain, invalid contacts, lost power, malformed serial data and communication expiry are unsafe. Valid dry data must persist for the configured delay (default 300 seconds). Rain state changes enqueue an immediate safety refresh without waiting for the two-second background heartbeat. NOAA never blocks the serial reader or an ASCOM call. Actual closure timing also depends on the automation client's polling and dome movement.

CloudCover returns the fraction of good-quality cloudy pixels in the configured region. Missing/stale/invalid NOAA data throws an ASCOM DriverException. SkyTemperature throws PropertyNotImplementedException because the IR sensor is removed. The associated sensor-description/update-time calls report it as not implemented. Weather-station RainRate and other numeric weather properties remain unchanged. See [NOAA details](NOAA-CLOUD.md).

## Safety-driver pairing

Keep the SafetyMonitor version that understands expiring `WW2RC1` records. Older drivers that interpret only the last character are unsuitable for Arduino mode. The file remains `C:\ProgramData\WeatherWatcher2\weatherdata.txt`, atomically replaced with `WW2RC1|UTC ticks|0 or 1`. SAFE is 0; UNSAFE is 1. Serial expiry is ten seconds; a stopped publisher's file also expires after ten seconds. Legacy mode remains unchanged.

ObservingConditions must remain connected in NINA (or another ASCOM client); SafetyMonitor alone does not start the serial publisher. Opening is prohibited and closure requested by the existing automation using IsSafe, not by a new direct dome command.

## Build and package

Run `./build.ps1` from the project with Visual Studio 2022, ASCOM Developer Components and the .NET Framework 4.7.2 targeting pack installed. It restores pinned NuGet dependencies, builds the ObservingConditions solution and complete SafetyMonitor project, builds tests and runs offline regressions. The safety build disables COM registration. The installer is compiled but not run; nothing is installed or flashed.

PureHDF 1.0.1 and its .NET compatibility DLLs must accompany the WeatherWatcher EXE and generated config. `installer/WeatherWatcher-Suite.iss` includes these dependencies. Build-only StrongNamer 0.2.5 adds a strong name to the PureHDF build copy so the existing strong-named WeatherWatcher executable can load it under .NET Framework. The NuGet source package remains unchanged. Tests invoke the decoder in the actual signed WeatherWatcher assembly against a real NOAA fixture. This is assembly compatibility signing, not Authenticode release signing.

Optional real-file validation: set `WEATHERWATCHER_NOAA_FIXTURE` to a downloaded ABI-L2-ACMF `.nc` file before running the tests. The normal suite is fully offline and does not open USB or write production safety files. The combined repository holds the C# driver in `WeatherWatcher2.ObservingConditions/` and the VB.NET driver in `SafetyMonitor/`. The build preserves the SafetyMonitor AnyCPU architecture guard.

New firmware must be paired with the new Windows parser. The new parser tolerates old IR fields, but an old firmware dry hold adds to the Windows delay. Existing profile settings are preserved; obsolete cloud safety thresholds are ignored. Configure real coordinates before expecting CloudCover.

Commission using [BENCH-TEST.md](BENCH-TEST.md). Binaries built here are development artifacts until installation and physical hardware tests pass.

## Optional Pushover cloud warnings

Version 1.2.1 adds **Cloud notifications...** in setup. Credentials are protected with Windows DPAPI. Warning/clearing thresholds and cooldown are configurable; unavailable/restored notices are optional. Notification delivery is asynchronous and has no safety-state access. See [PUSHOVER.md](PUSHOVER.md).
