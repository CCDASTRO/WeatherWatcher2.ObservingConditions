# CCDASTRO WeatherWatcher Suite

One repository for **WeatherWatcher ObservingConditions**, **WeatherWatcher SafetyMonitor**, and the optional **Arduino / Hydreon RG-11 rain detector**. Version **1.2.1** adds NOAA cloud information and Pushover advisories while preserving SafetyMonitor **1.3.1**, its AnyCPU compatibility fix, and the existing weather sources.

**Rain controls safety. Satellite clouds and notifications do not.** The Arduino IR sky-temperature sensor has been removed from the current firmware and Arduino-mode processing.

[Setup guide](docs/weatherwatcher-guide.html) · [Pushover setup](docs/RainCloud/PUSHOVER.md) · [NOAA details](docs/RainCloud/NOAA-CLOUD.md) · [Available releases](https://github.com/CCDASTRO/WeatherWatcher2.ObservingConditions/releases)

## What's new

- **Local RG-11 protection:** Uno monitors both relay contacts and the isolated sensor-power signal. Rain/wet output immediately reports UNSAFE through the existing safety path. Power/contact faults, lost serial communication and restarts also prevent SAFE.
- **Configurable dry-out:** Windows requires uninterrupted valid dry readings, default five minutes. New firmware removes the old additional board-side five-minute hold.
- **NOAA replaces IR cloud sensing:** Windows retrieves GOES-East ABI Level-2 Clear Sky Mask data and reports regional ASCOM CloudCover. No rendered-image analysis or Arduino Internet connection is involved.
- **Informational cloud data:** cloudy, missing or stale NOAA observations and NOAA download failures never veto safety. Existing independent weather checks remain; a separately selected Ambient API source retains its existing failure behavior.
- **Pushover advisories:** optional cloud/clearing notices, configurable thresholds and cooldown, optional unavailable/recovered notices, protected credentials and a test button. Notification failure cannot alter safety or delay rain processing.

WeatherWatcher does not issue dome commands. Existing automation must prohibit opening while UNSAFE and close an open dome on UNSAFE.

## Sources and properties

| Source | Purpose | Safety behavior |
| --- | --- | --- |
| Arduino + RG-11 | Local rain/wet flag and power/contact health | Rain, faults, stale serial data and dry-out hold are UNSAFE |
| NOAA GOES-East | Regional CloudCover and observation age | Informational only |
| Cumulus or Ambient | Existing temperature, humidity, wind, pressure and rainfall | Existing limits and source-health behavior retained |
| Boltwood file | Legacy rain/sky input when Arduino mode is disabled | Existing behavior retained |
| Pushover | Optional phone/device cloud warnings | No safety authority |

Arduino mode overrides Boltwood as before. With it disabled, no Uno port is opened and existing file/API modes remain available. Installation does not enable Arduino or Pushover automatically.

In Arduino mode **SkyTemperature is not implemented**. CloudCover throws an ASCOM error when NOAA data is unavailable/stale; it never substitutes zero. RainRate remains the selected weather source's measurement and is not fabricated from a relay flag.

## Setup

1. Close NINA and all clients using either driver before installation. Requirements: Windows 10+, ASCOM Platform 7.1+, and .NET Framework 4.8. See [Releases](https://github.com/CCDASTRO/WeatherWatcher2.ObservingConditions/releases) for published packages; building current source creates its installer in `dist/`.
2. Compile and bench-test the [rain-only Uno sketch](firmware/RainCloudUno/RainCloudUno.ino) using Arduino AVR Boards. No Adafruit, MLX90614 or Wire library is needed. Remove the IR module and its VIN/GND/SDA/SCL wires. Retain RG-11 and power-monitor wiring from the [current hardware guide](docs/RainCloud/HARDWARE.md).
3. Enable **Use Arduino RG-11 rain protection and NOAA cloud information** in WeatherWatcher setup. Enter the COM port and dry-out delay.
4. Enter observatory latitude/longitude in decimal degrees: north/east positive, west negative. No location is assumed. Leave both blank for rain-only operation; cloud data remains unavailable.
5. Keep Cumulus or Ambient configured as desired. In NINA select WeatherWatcher2 ObservingConditions and WeatherWatcher SafetyMonitor. Keep ObservingConditions connected to maintain safety output. Close Arduino Serial Monitor so it releases the COM port.
6. Point SafetyMonitor at `C:\ProgramData\WeatherWatcher2\weatherdata.txt`. Verify opening inhibition and closure with your automation before unattended use.

| Setting | Default | Range |
| --- | --- | --- |
| RG-11 dry-out | 300 seconds | 0–3600 seconds; zero disables the extra Windows hold |
| NOAA sampling radius | 15 km | 5–100 km |
| NOAA polling | 600 seconds | 600–3600 seconds |
| Cloud stale timeout | 1800 seconds | 600–86400 seconds, at least the polling interval |
| Pushover warning | 30% or greater | 1–100%, above clearing threshold |
| Pushover clearing | 20% or less | 0–99%, below warning threshold |
| Notification cooldown | 30 minutes | 1–1440 minutes |

USB remains JSON v1 at 9600 baud, with a two-second heartbeat, immediate state-change reports and existing `get#` / `getd#` commands. The new Windows parser accepts old IR fields but ignores them. Old firmware adds its own hold before Windows recovery; upgrade both components. Old Windows binaries that require IR fields cannot consume the new rain-only firmware.

The safety record remains `WW2RC1|UTC ticks|0 or 1`: SAFE is 0; UNSAFE is 1. Serial observations and the safety file each expire after ten seconds. Overall response time also includes automation polling and dome travel. Use the matching SafetyMonitor that understands expiry, not an old last-character-only reader.

## NOAA cloud information

The provider uses the public `noaa-goes19` bucket and `ABI-L2-ACMF` full-disk NetCDF product, normally updated every ten minutes. It reads the binary cloud mask and quality flags, samples pixel centers within the geographic radius, and calculates `100 × cloudy good-quality pixels / all good-quality pixels`. At least three pixels and 80% good-quality coverage are required. Bad/fill pixels never become clear observations.

Downloads/decoding run in the background. All clients share a cache; unchanged products are not downloaded again. ASCOM reports the satellite acquisition age, not the download age. Retrieval failures preserve the previous valid result only until it expires. Regional cloud fraction is not rain detection, a local all-sky measurement or a forecast. [Algorithm, source and limitations](docs/RainCloud/NOAA-CLOUD.md).

## Pushover cloud advisories

Choose **Cloud notifications...** in setup. Enter your Pushover application token and user/group key, optionally select a device, and enable notifications. Both keys are masked and encrypted with Windows DPAPI for the current user. No shared credentials are shipped.

Fresh observations trigger warnings at the high threshold and clearing notices at the lower threshold. The gap prevents repeated alerts near the cutoff. Cooldown and duplicate suppression are shared by ASCOM clients. Clearing notices default on; unavailable/recovered notices default off. Normal-priority messages respect quiet hours. An Internet outage may prevent notification delivery but cannot affect local RG-11 protection.

**Send test notification** sends a real, clearly labeled test. Click OK in both dialogs to save. Automatic advisories require connected ObservingConditions, Arduino/NOAA mode and valid site coordinates. [Full setup and behavior](docs/RainCloud/PUSHOVER.md).

## Build and validation

Install Visual Studio 2022 Community with .NET desktop tools, the .NET Framework 4.7.2 targeting pack, ASCOM Developer Components and Inno Setup 6. Place your existing local SafetyMonitor signing key at `SafetyMonitor/ASCOMDriverTemplate.snk`; it is excluded from Git. Run:

```powershell
./build.ps1
```

The script restores pinned NuGet dependencies, builds both solutions without COM registration, verifies SafetyMonitor AnyCPU architecture, runs offline tests and creates the installer. PureHDF decodes NetCDF/HDF5; build-only StrongNamer supplies the strong name required by the existing executable. Runtime DLLs and license notices are packaged.

**219 offline checks pass**, including mocked Pushover delivery and rain safety during a stalled notification. No real Pushover messages were sent. Both Windows configurations and the installer build successfully. Uno compilation also passed. The preceding NOAA change verified a real file, an independent full-grid calculation and live retrieval. These results do not establish physical wiring, phone delivery, installation or dome qualification. Follow the [bench checklist](docs/RainCloud/BENCH-TEST.md).

## Layout and history

| Folder | Contents |
| --- | --- |
| `WeatherWatcher2.ObservingConditions/` | C# ASCOM server, USB reader, NOAA and Pushover |
| `SafetyMonitor/` | VB.NET SafetyMonitor with preserved 1.3.1 AnyCPU fix |
| `firmware/RainCloudUno/` | RG-11-only sketch and timing configuration |
| `tests/` | Offline regressions and optional real/live NOAA checks |
| `installer/` | Combined Inno Setup package |
| `docs/` | Setup, wiring, protocol and commissioning guides |
| `website/` | CCDASTRO upload files; pushing does not deploy the live site |

Historical IR wiring/Fritzing files are retained as design history, not current assembly instructions. Follow the current wiring table. The [SNFSafety repository](https://github.com/CCDASTRO/SNFSafety) remains historical; combined development lives here. Avoid old standalone uninstallers after installing the suite, as they can remove its shared SafetyMonitor DLL.

- **1.2.1:** Pushover cloud advisories, protected credentials and configuration.
- **1.2.0:** rain-only Arduino, configurable dry-out, NOAA clouds and logging.
- **1.1.1 suite / SafetyMonitor 1.3.1:** preserved AnyCPU registration fix and architecture guard; previously verified 32/64-bit activation.
- **1.1.0:** original combined suite and optional Arduino prototype.

Retained Ambient key instructions and legacy configuration: [previous setup guide](docs/WeatherWatcher-previous-setup.md).
