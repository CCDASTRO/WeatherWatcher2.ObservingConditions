# WeatherWatcher2 Observing Conditions

WeatherWatcher2 is an ASCOM Observing Conditions driver for Windows. It supplies
weather and sky readings to ASCOM-compatible astronomy applications and writes a
weather safety status file for external consumers.

## Version 1.0.3

- Keeps the ASCOM setup dialog centered and owned by the foreground ASCOM client,
  with topmost and taskbar fallbacks, so NINA cannot hide it.
- Adds a setup-dialog reminder with a link to the GitHub releases page.
- Warns users to close NINA before installing a WeatherWatcher update.

## Version 1.0.1
- Adds AmbientWeather.net as an optional alternative to Cumulus.
- Retains the existing file readers and independent Boltwood selection.
- Adds protected credentials, cached background API reads, and observation-age checks.
- Saves source selections and documents setup, safety limitations, and deployment.
- Includes offline regression and setup-dialog checks.

The Inno Setup installer is built separately; this repository update does not
include a newly built installer.

## Data sources

| Source | Configuration | Can be combined with |
| --- | --- | --- |
| Cumulus MX | Enable Cumulus and select its realtime.txt file | Boltwood |
| AmbientWeather.net API | Select **Use Ambient API instead of Cumulus**, then enter keys and station MAC | Boltwood |
| Boltwood-compatible file | Enable Boltwood and select the sensor output file | Cumulus or Ambient, or use alone |

The existing file readers are retained. Existing WeatherWatcher2 files such as
currdat.lst, currdat.txt, and WeatherWatcher.txt can still be selected through the
file inputs when their contents match the supported layout. The driver parses
file contents; a filename alone does not identify or convert a format.

Ambient is optional and disabled by default. Enabling it bypasses the Cumulus
reader without deleting its path or enable preference. Boltwood is independently
selectable in either mode. No Ambient credentials are required for file mode.

## Reported conditions

Depending on the selected source and available sensors, the driver provides:

- Ambient temperature, relative humidity, and dew point.
- Wind speed, gust speed, and direction.
- Barometric pressure and rain rate or the existing file-mode rain/wetness indication.
- Sky temperature and estimated cloud cover from the Boltwood input.
- Time since the last update; Ambient mode uses the actual observation timestamp.

Sky brightness, sky quality, and star FWHM are currently not implemented.
Ambient mode does not invent readings for missing sensors. Without Boltwood,
sky temperature and cloud cover are unavailable.

## Setup and compatibility

Use the driver's ASCOM setup dialog to select file paths, enable data sources,
configure weather thresholds, and enable optional trace logging. The driver
retains the existing ASCOM identity; clients do not need a separate Ambient driver.

The Ambient addition does not rewrite the existing file parsers. In particular,
file-mode unit handling, rain representation, and freshness behavior remain
legacy behavior and should be validated with your actual input files. The
maximum-temperature setting is saved, but the existing safety evaluator currently
checks minimum temperature, wind speed, humidity, and rain/wetness; it does not
enforce that maximum-temperature setting or a cloud-cover threshold.

## Safety output

The driver writes C:\ProgramData\WeatherWatcher2\weatherdata.txt:

- **0** means the current safety evaluation passed.
- **1** means unsafe.

Keep sensor input files separate from this output. In particular, do not point
the Boltwood input at this safety file: it contains only a status digit, not a
weather record, and is overwritten during refresh.

The consumer of this file must independently reject a missing or outdated file
if the driver stops or disconnects. This output is not a substitute for an
independent local rain/wetness safeguard.

## Optional AmbientWeather.net source

Cumulus and Boltwood file inputs remain available. Existing installations default
to file mode; no Ambient account or keys are needed unless you enable the option.
The existing file parsers and their supported producer formats are retained.

To replace Cumulus with Ambient Weather:

1. Ensure your station is uploading to AmbientWeather.net.
2. Follow [How to generate your Ambient API keys](#how-to-generate-your-ambient-api-keys) below to create both keys on your
   [Ambient account page](https://ambientweather.net/account).
3. In the driver's setup dialog, select **Use Ambient API instead of Cumulus**.
4. Enter both keys and the station MAC address, for example AA:BB:CC:DD:EE:FF.
5. Leave **Enable Boltwood** selected if you also use that file source. Otherwise,
   disable it; Ambient does not provide sky temperature or cloud cover.
6. Set the maximum observation age (default 300 seconds), save, and reconnect.
   Initial readings may take several seconds; retry if data is not ready yet.

Uncheck the Ambient option to return to the retained Cumulus path and enable
setting. Ambient never silently falls back to Cumulus during an outage. Source
enable settings and paths are saved independently.

Keys are masked in setup and stored using Windows DPAPI for the current Windows
user. Re-enter them if running the driver under another Windows account. Do not
post keys in issues, screenshots, or source control.

### How to generate your Ambient API keys

**Two different keys are required.** Your personal API key authorizes access to
your station data; an application key identifies the application making the
request. Ambient's support pages also call the personal key a **User Key** or
**Device Key**. WeatherWatcher labels that field **API key**.

WeatherWatcher does not include a shared application key. With this version,
each user supplies both keys in setup. You do not need to write software to
configure WeatherWatcher, but you do need to complete Ambient's application-key
creation step.

#### 1. Create your personal API key

1. Sign in to the [AmbientWeather.net account page](https://ambientweather.net/account)
   with the account that owns your station. This is the weather dashboard account.
2. Find the **API Keys** section and use its key-generation control.
3. Copy the generated long key string. This is your **personal API key**; it goes
   into WeatherWatcher's **API key** field.

#### 2. Create a separate application key

1. In the same API Keys area, look for the developer/application-key link.
   The wording may be similar to: **Each developer also needs an application
   key. Click here to create one.** Dashboard wording and layout can change.
2. Follow that link and complete the requested application information. If asked
   to describe the integration, use an accurate description such as:

   > I am connecting my own Ambient Weather station to the WeatherWatcher2 ASCOM
   > ObservingConditions driver so my astronomy software can read weather conditions.

3. Submit the form and look for the resulting **application key** entry
   Ambient creates. Copy its key string into WeatherWatcher's **Application key**
   field.

Both keys can look like long hexadecimal strings. The application key may appear
as a second row in the same table, marked **(application key)**. Identify it by
that label, not its row number: row order can vary, especially if you have created
keys before. Do not paste the same personal key into both fields.

#### 3. Enter the keys in WeatherWatcher

| WeatherWatcher field | What to enter |
| --- | --- |
| Application key | The separate key identified as an application key |
| API key | Your personal API/User/Device key |
| Station MAC | Your station's MAC address, such as AA:BB:CC:DD:EE:FF; this is not a key |

Select **Use Ambient API instead of Cumulus**, enter all three values, save, and
reconnect your ASCOM client. Keep Boltwood enabled only if you use that file input.
Neither key is needed when using only Cumulus or Boltwood.

If you see only one key, revisit the application-key link rather than generating
another personal key. If requests fail, check that the keys are in the correct
fields, were copied completely without surrounding text, and belong to the
intended integration/account; also verify the station MAC and that the station
is uploading. WeatherWatcher limits requests to once per minute, so allow the
next polling interval after correcting settings.

Keep both keys private. Do not include them in screenshots, GitHub issues, or
source control. If a key is exposed, replace/revoke it in Ambient and update
WeatherWatcher's saved value.

Official references:
[Ambient's key-creation guide, including its video](https://ambientweather.com/faqs/question/view/id/1934/)
and [Ambient REST API authentication documentation](https://github.com/ambient-weather/api-docs/blob/master/apiary.apib).
These explain the two key types and account-based creation; the dashboard
navigation hints above may differ as Ambient updates its interface.

### Data handling and safety

- The driver requests the latest /v1/devices data at most once per minute per
  server process and selects the configured station MAC. Calls run in the
  background; ASCOM properties share the cached observation.
- While connected in Ambient mode, a five-second timer updates the snapshot and
  safety output even if no client is reading individual weather properties.
- Temperature and dew point are converted to Celsius, wind to m/s, absolute
  pressure to hPa, and hourly rain rate to mm/hour. Missing fields are not zero.
- Observation age comes from Ambient's station timestamp, not the HTTP response
  time. Failed requests do not refresh it. Missing, failed, or stale Ambient
  safety data makes the safety output unsafe. Weather properties raise errors
  for unavailable/stale data; unsupported sensors raise not-implemented errors.
- Temperature, humidity, wind speed, and rain rate are required for Ambient mode
  to report safe. Enabled Boltwood data must also be present and fresh.
- Boltwood rain/wetness still marks unsafe, without replacing Ambient's numerical
  rain rate. The existing file-mode rain representation and freshness behavior
  are unchanged by this feature.
- Cloud/API connectivity and upload delays make this unsuitable as the sole
  protection against rain. Keep an independent local rain/wetness safeguard.

References:
[Ambient REST API](https://github.com/ambient-weather/api-docs/blob/master/apiary.apib)
and [field definitions](https://github.com/ambient-weather/api-docs/wiki/Device-Data-Specs).

### Verification

Build the solution in Release using Visual Studio/MSBuild with the ASCOM Platform
and .NET Framework 4.7.2 development tools installed. No extra NuGet dependency is
required. Then build and run the offline regression harness:

    msbuild WeatherWatcher2.ObservingConditions.sln /t:Build /p:Configuration=Release /p:Platform="Any CPU"
    msbuild tests\AmbientTests.csproj /t:Build
    tests\bin\AmbientTests.exe

Tests use simulated API responses and temporary weather files, without calling
Ambient, changing the ASCOM profile, or writing the production safety file.
A live station/credential check and ASCOM client acceptance test are still
required before observatory deployment.

### Deployment checklist

1. Build the updated driver and run the offline tests.
2. Close connected ASCOM clients before replacing the installed driver using
   your normal deployment process. Building alone does not install or enable it.
3. Open setup, choose the desired source, and save. For Ambient, enter credentials
   locally in the dialog rather than in source files or chat.
4. Reconnect an ASCOM client and check readings, units, observation age, and the
   safety output against the actual station.
5. Verify an internet outage or stale station upload produces an unsafe result
   in Ambient mode, then verify recovery.
6. Uncheck Ambient and confirm the retained Cumulus and Boltwood inputs work.

The offline suite covers conversions, missing/invalid observations, caching,
request failures, source switching, Boltwood coexistence, credential protection,
and setup controls. It does not replace a live API or observatory acceptance test.

## Installation and client configuration

Use Windows with ASCOM Platform 7 or later. Building from source also requires
the .NET Framework 4.7.2 development tools.

1. Install ASCOM Platform.
2. Install WeatherWatcher2 using the separately built installer.
3. Open the ASCOM Chooser in your astronomy application.
4. Select WeatherWatcher2 ObservingConditions and open Setup.
5. Configure the file inputs or optional Ambient API source.
6. Connect and verify readings before enabling automation.

ASCOM clients such as NINA, Voyager, ACP, TheSkyX, and Sequence Generator Pro can
consume the driver's ObservingConditions interface. Verify compatibility with
your particular client and configuration.

### ASCOM profile settings

Configuration is stored using the ASCOM Profile system.

| Setting | Purpose |
| --- | --- |
| BoltwoodFile / CumulusFile | Retained input file paths |
| UseBoltwood / UseCumulus | Independent file-source preferences |
| UseAmbient | Select Ambient instead of Cumulus |
| AmbientApplicationKeyProtected / AmbientApiKeyProtected | DPAPI-protected credentials |
| AmbientMacAddress | Selected station |
| AmbientMaxAgeSeconds | Ambient-mode maximum observation age |
| EnableLogging | Trace logging preference |
| MaxWind / MaxHumidity / MinTemp / MaxTemp | Saved threshold settings; see safety limitations above |

### Network access through ASCOM Remote

The driver is a Windows ASCOM local server, not a native Alpaca server. ASCOM
Remote can expose compatible local ASCOM devices to network clients; validate
your Remote configuration and client behavior before observatory deployment.

## License

Copyright © WeatherWatcher2 Project.

All rights reserved unless otherwise specified.

## Support

Report bugs, request features, or suggest documentation changes through
[GitHub Issues](https://github.com/CCDASTRO/WeatherWatcher2.ObservingConditions/issues).
Do not include API keys in reports or logs.