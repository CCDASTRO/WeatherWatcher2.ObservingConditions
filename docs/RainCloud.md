# ASCOM integration — development build

Implemented source versions: WeatherWatcher2 ObservingConditions **1.1.0.0** and WeatherWatcher SafetyMonitor **1.3.0.0**. Both were built successfully; 126 checks passed without opening a serial port, making a live weather API request or writing production safety files. This is not a hardware-qualified release and is published as a development prerelease, not installed or hardware-qualified.

## Data path
Uno USB at 9600 baud -> one shared serial reader in the WeatherWatcher local server -> ObservingConditions sky/cloud properties and combined safety decision -> timestamped safety file -> WeatherWatcher SafetyMonitor -> NINA.

In WeatherWatcher setup, enable **Use Uno RainCloud instead of Boltwood rain/cloud input**, enter the Uno COM port and set the maximum cloud estimate. Existing Ambient/Cumulus weather inputs stay in use. This input overrides Boltwood rain/cloud data while enabled. Do not run Arduino Serial Monitor while WeatherWatcher owns that port.

The Uno must be loaded and calibrated before the system can report safe. The RG-11 dry hold runs on the board; a separate five-minute clear recovery runs in the Windows reader. Faults and reconnects restart recovery. Cloud percentages represent the sensor's calibrated field of view, not measured whole-sky coverage.

SkyTemperature and CloudCover expose valid Uno values. Invalid/stale readings throw an ASCOM error. RainRate is not synthesized from the relay, and air temperature is not replaced by the IR chip temperature. Other weather measurements and their limits remain part of the combined decision.

## Safety-driver pairing
Install the updated SafetyMonitor alongside the updated ObservingConditions driver before enabling RainCloud. The older SafetyMonitor interprets only the last character and does not enforce the new expiry. **Using the old safety DLL is not supported with RainCloud enabled.**

The output remains `C:\ProgramData\WeatherWatcher2\weatherdata.txt`, with RainCloud-enabled records formatted `WW2RC1|UTC ticks|0 or 1`. 0 means combined safe; 1 means unsafe. The new safety reader rejects records 10 seconds old or from the future. Files are replaced atomically. Legacy non-RainCloud data retains the existing safety behavior.

The reader rejects records with wrong version/types/ranges, invalid relay combinations, lost power, repeated/backwards counters, missing fields, stale IR acquisition or sensor faults. Disconnecting the source and reader stalls result in unsafe. WeatherWatcher polls safety every two seconds while connected. A sensor stream timeout is ten seconds; the separate safety-file timeout is ten seconds after the last published result. Overall detection latency includes those stages and the client's own polling interval.

If only the SafetyMonitor is connected, it does not open the Uno or launch WeatherWatcher automatically. Keep ObservingConditions connected in NINA to maintain the combined result. A stopped WeatherWatcher expires unsafe.

## Build
Use Visual Studio MSBuild with ASCOM Developer Components and .NET Framework 4.7.2 targeting pack installed. Run `build.ps1` from this project. The safety project must use a fresh intermediate directory and `RegisterForComInterop=false`; do not register test builds as a side effect of compilation.

Source is consolidated under `WeatherWatcher2.ObservingConditions/` and `SafetyMonitor/`, with Uno firmware in `firmware/RainCloudUno/`. The `dist` folder contains both a development ZIP and a combined Inno installer, CCDASTRO.WeatherWatcher-Safety.Setup-1.1.0-development.exe. It installs WeatherWatcher 1.1.0 and SafetyMonitor 1.3.0 together and preserves ASCOM profile settings. RainCloud defaults to disabled. The installer has been compiled but not installed or upgrade-tested. Older standalone SafetyMonitor uninstall entries may remain; do not run those after installing the suite, as they can remove the shared safety DLL. Retain the working installed drivers until bench acceptance passes.

## Next hardware checks
1. Upload and verify the Uno sketch; test both relay states, power loss and IR unplug/recovery.
2. After deliberate installation of both matching drivers, connect WeatherWatcher to the Uno and observe live sky/rain/cloud data.
3. Confirm NINA SafetyMonitor remains unsafe while uncalibrated, during dry/clear holds, on unplug, and when WeatherWatcher closes.
4. Calibrate cloud thresholds at the final mount and conduct a prolonged parallel trial before relying on this input.
