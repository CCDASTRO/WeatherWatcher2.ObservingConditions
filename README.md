# CCDASTRO WeatherWatcher Suite

One repository and one Windows installer for **WeatherWatcher ObservingConditions**, **WeatherWatcher SafetyMonitor**, and the optional **RainCloud Arduino sensor**.

The current development suite pairs WeatherWatcher **1.1.0** with SafetyMonitor **1.3.1**. It is not yet hardware-qualified or installation-tested. Existing stable releases remain available in [Releases](https://github.com/CCDASTRO/WeatherWatcher2.ObservingConditions/releases).

## With or without the optional sensor

**RainCloud is disabled by default.** No Uno COM port is opened while disabled; existing AmbientWeather, Cumulus and Boltwood configuration remains in use. Installing the suite does not enable the sensor or overwrite ASCOM profile settings.

When enabled, an Uno R3 reads a Hydreon RG-11 rain detector and GY-906 MLX90614 infrared sensor. WeatherWatcher receives USB telemetry and exposes sky temperature and a calibrated cloud estimate. RainCloud replaces the Boltwood rain/cloud input, while existing weather measurements and limits remain part of the combined safety decision.

Missing, stale, invalid or uncalibrated sensor data prevents a safe result. Disable RainCloud while disconnected if the sensor is not being used. The cloud estimate describes the sensor's field of view, not measured whole-sky coverage or a forecast. No artificial rainfall rate is generated from the relay.

## Install and use

The combined Inno installer contains both ASCOM drivers. Use the matching SafetyMonitor because older versions do not enforce the new safety-record expiry. Requirements: Windows 10+, ASCOM Platform 7.1+ and .NET Framework 4.8. Close NINA and all WeatherWatcher/SafetyMonitor clients before installation.

In NINA select WeatherWatcher2 for Observing Conditions and WeatherWatcher SafetyMonitor for Safety Monitor. Keep Observing Conditions connected to maintain the combined safety output. With RainCloud enabled, select the Uno COM port and cloud limit in WeatherWatcher setup. Arduino Serial Monitor must be closed. SafetyMonitor reads `C:\ProgramData\WeatherWatcher2\weatherdata.txt`.

The sensor's dry recovery and Windows clear recovery each take five minutes. Faults reset recovery. Sensor data and the combined safety file each have a ten-second expiry; total response latency includes both stages and the client polling interval. Existing independent weather protection should remain active during commissioning.

## Repository layout

| Folder | Contents |
| --- | --- |
| `WeatherWatcher2.ObservingConditions/` | C# ASCOM local server and shared USB reader |
| `SafetyMonitor/` | VB.NET ASCOM SafetyMonitor and expiring-result validation |
| `firmware/RainCloudUno/` | Uno sketch and site cloud-calibration configuration |
| `installer/` | Combined Inno Setup script |
| `tests/` | Offline weather-source, sensor, and safety regression tests |
| `docs/` | Usage, wiring, protocol and commissioning guides |

[Usage guide](docs/weatherwatcher-guide.html) · [ASCOM integration](docs/RainCloud/ASCOM-INTEGRATION.md) · [Wiring and parts](docs/RainCloud/HARDWARE.md) · [USB protocol](docs/RainCloud/PROTOCOL.md) · [Bench checklist](docs/RainCloud/BENCH-TEST.md)

RainCloud wiring: [view the circuit diagram](docs/RainCloud/images/raincloud-wiring.svg), [view the Fritzing layout](docs/RainCloud/images/raincloud-fritzing-layout.png), or [download the editable Fritzing project](docs/RainCloud/raincloud-wiring.fzz). Confirm pin labels and follow the [hardware guide](docs/RainCloud/HARDWARE.md) before assembly.

## Build and validation

Install Visual Studio 2022 Community with .NET desktop build tools and the .NET Framework 4.7.2 targeting pack, ASCOM Developer Components, and Inno Setup 6. Place your existing SafetyMonitor `ASCOMDriverTemplate.snk` locally in `SafetyMonitor/` before building; signing material is intentionally excluded from this repository. Run `./build.ps1`; adjust the MSBuild path if using another Visual Studio edition. The script builds both drivers without COM registration, runs tests and creates the installer under `dist/`.

126 offline checks pass. They do not open physical serial ports, call live weather APIs or write production safety files. Uno compilation, physical commissioning, installation and upgrade testing remain outstanding. These are development results, not ASCOM certification.

The previous [SNFSafety repository](https://github.com/CCDASTRO/SNFSafety) remains available for historical source and releases. Future combined development lives here. Older standalone uninstall entries may remain after upgrading; do not use them to remove the shared safety DLL after installing this suite.

### Preview 2 installer correction
Suite installer 1.1.1 fixes SafetyMonitor Release architecture to AnyCPU (SafetyMonitor 1.3.1). Installation completed successfully on the development PC, with 32-bit and 64-bit COM activation/disposal verified. Hardware commissioning remains outstanding. A build guard now rejects architecture-incompatible SafetyMonitor packages.
