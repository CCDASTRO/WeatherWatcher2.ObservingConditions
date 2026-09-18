using ASCOM.Utilities;
using System;
using System.IO;

namespace WeatherWatcher2.ObservingConditions
{
    public class WeatherDataReader
    {
        private readonly TraceLogger tl;
        private readonly object gate = new object();
        private bool ambientHealthy, cumulusHealthy;
        private string sourceSelection;
        private DateTime boltwoodUpdateUtc = DateTime.MinValue;
        private DateTime ambientUpdateUtc = DateTime.MinValue;
        private string ambientError;
        internal Func<RainCloudView> RainCloudRead = RainCloudHub.Read;
        private RainCloudView rainCloud;
        internal CloudNotifier Notifier = CloudNotifier.Shared;
        internal bool NotificationsActive;
        internal NoaaCloudClient NoaaClient = NoaaCloudClient.Shared;
        private CloudReading cloudReading;
        private string cloudError, lastRainStatus, lastCloudStatus;
        private bool? previousSafe;
        internal Action<bool> SafetyWriter { get; set; }
        internal AmbientWeatherClient AmbientClient = AmbientWeatherClient.Shared;

        public WeatherSnapshot Data { get; private set; }

        public WeatherDataReader(TraceLogger logger)
        {
            tl = logger;
            Data = new WeatherSnapshot();
        }

        public void Refresh()
        {
            lock (gate)
            {
                string selection = DriverSettings.UseRainCloud + "|" + DriverSettings.UseAmbient + "|" + DriverSettings.UseCumulus + "|" +
                    DriverSettings.UseBoltwood + "|" + DriverSettings.CumulusFile + "|" + DriverSettings.BoltwoodFile;
                if (sourceSelection != selection)
                {
                    sourceSelection = selection;
                    Data = new WeatherSnapshot();
                }
                if (DriverSettings.UseAmbient)
                {
                    // A new snapshot prevents values from a previous source or missing fields leaking through.
                    Data = new WeatherSnapshot();
                    ReadAmbient();
                }
                else if (DriverSettings.UseCumulus)
                {
                    ReadCumulus();
                }
                if (DriverSettings.UseBoltwood && !DriverSettings.UseRainCloud) ReadBoltwood();
                if (DriverSettings.UseRainCloud) ReadRainCloud();
                EvaluateSafeState();
                if (DriverSettings.UseRainCloud) Data.IsSafe = Data.IsSafe && rainCloud != null && rainCloud.Safe &&
                    (DriverSettings.UseAmbient || !DriverSettings.UseCumulus || (cumulusHealthy && !IsFileStale(DriverSettings.CumulusFile, DriverSettings.AmbientMaxAgeSeconds)));
                if (previousSafe != Data.IsSafe)
                {
                    tl?.LogMessage("Safety transition", (Data.IsSafe ? "SAFE" : "UNSAFE") +
                        "; rain=" + Data.RainUnsafe + "; wind=" + Data.WindUnsafe + "; humidity=" + Data.HumidityUnsafe +
                        "; temperature=" + Data.TemperatureUnsafe + "; RG-11=" + (rainCloud == null ? "disabled" : rainCloud.Error ?? (rainCloud.Safe ? "dry" : "dry-out pending")));
                    previousSafe = Data.IsSafe;
                }
                if (SafetyWriter != null) SafetyWriter(Data.IsSafe);
                else WriteSafetyFile();
                // Notification evaluation follows safety publication and never performs network I/O here.
                if (DriverSettings.UseRainCloud && NotificationsActive)
                {
                    try { Notifier.Process(cloudReading, DateTime.UtcNow, DriverSettings.NoaaStaleSeconds, NoaaSite.Current, DriverSettings.Pushover); }
                    catch { /* Advisory failures must never interfere with rain protection. */ }
                }

                if (!DriverSettings.UseAmbient)
                    Data.LastUpdateUtc = DateTime.UtcNow; // Preserve the existing file-mode behavior.
            }
        }

        private void ReadRainCloud()
        {
            rainCloud = RainCloudRead();
            Data.SkyTemperature = double.NaN; // No IR sensor is installed.
            Data.RainUnsafe = Data.RainRate > 0 || !rainCloud.Safe;
            Data.CloudUnsafe = false; // Satellite clouds never participate in safety.
            string rainStatus = rainCloud.Rain + "|" + rainCloud.Fresh + "|" + rainCloud.Safe + "|" + rainCloud.Error;
            if (rainStatus != lastRainStatus)
            {
                lastRainStatus = rainStatus;
                tl?.LogMessage("RG-11", rainStatus + "; sample age " + rainCloud.AgeSeconds.ToString("F1") + "s");
            }
            cloudReading = NoaaClient.Get(NoaaSite.Current, DateTime.UtcNow, DriverSettings.NoaaPollSeconds, out cloudError);
            bool valid = cloudReading != null && cloudReading.IsFresh(DateTime.UtcNow, DriverSettings.NoaaStaleSeconds);
            Data.CloudCover = valid ? cloudReading.Percent : double.NaN;
            string cloudStatus = valid ? cloudReading.ObservationUtc.ToString("O") : "Unavailable: " + (cloudError ?? "NOAA observation is stale");
            if (cloudStatus != lastCloudStatus)
            {
                lastCloudStatus = cloudStatus;
                tl?.LogMessage("NOAA cloud", cloudStatus + (cloudReading == null ? "" :
                    "; age=" + (DateTime.UtcNow - cloudReading.ObservationUtc).TotalSeconds.ToString("F0") + "s; cloud=" + cloudReading.Percent.ToString("F1") + "%"));
            }
        }
        private void ReadAmbient()
        {
            string error;
            var reading = AmbientClient.Get(DriverSettings.AmbientApplicationKey,
                DriverSettings.AmbientApiKey, DriverSettings.AmbientMacAddress, out error);
            ambientError = error;
            ambientUpdateUtc = reading == null ? DateTime.MinValue : reading.TimestampUtc;
            Data.LastUpdateUtc = ambientUpdateUtc;
            ambientHealthy = reading != null && error == null &&
                reading.IsFresh(DateTime.UtcNow, DriverSettings.AmbientMaxAgeSeconds) && reading.HasSafetyData;
            Data.Temperature = reading == null ? double.NaN : reading.Temperature;
            Data.DewPoint = reading == null ? double.NaN : reading.DewPoint;
            Data.Humidity = reading == null ? double.NaN : reading.Humidity;
            Data.Pressure = reading == null ? double.NaN : reading.Pressure;
            Data.WindSpeed = reading == null ? double.NaN : reading.WindSpeed;
            Data.WindGust = reading == null ? double.NaN : reading.WindGust;
            Data.WindDirection = reading == null ? double.NaN : reading.WindDirection;
            Data.RainRate = reading == null ? double.NaN : reading.RainRate;
            Data.RainUnsafe = reading != null && reading.RainRate > 0;
            Data.CloudCover = double.NaN;
            Data.SkyTemperature = double.NaN;
            boltwoodUpdateUtc = DateTime.MinValue;
        }

        public double ReadValue(string propertyName)
        {
            lock (gate)
            {
                Refresh();
                if (DriverSettings.UseRainCloud && propertyName == "SkyTemperature")
                    throw new ASCOM.PropertyNotImplementedException(propertyName, false);
                if (DriverSettings.UseRainCloud && propertyName == "CloudCover")
                {
                    if (double.IsNaN(Data.CloudCover)) throw new ASCOM.DriverException(cloudError ?? "NOAA cloud data is unavailable or stale.");
                    return Data.CloudCover;
                }
                if (DriverSettings.UseAmbient)
                {
                    bool sky = propertyName == "CloudCover" || propertyName == "SkyTemperature";
                    if (sky && !DriverSettings.UseBoltwood)
                        throw new ASCOM.PropertyNotImplementedException(propertyName, false);
                    DateTime timestamp = sky ? boltwoodUpdateUtc : ambientUpdateUtc;
                    if (timestamp == DateTime.MinValue || (DateTime.UtcNow - timestamp).TotalSeconds > DriverSettings.AmbientMaxAgeSeconds ||
                        (!sky && ambientError != null))
                        throw new ASCOM.DriverException(!sky && ambientError != null ? ambientError : "Weather data is unavailable or stale.");
                    if (double.IsNaN(Value(propertyName)))
                        throw new ASCOM.PropertyNotImplementedException(propertyName, false);
                }
                return Value(propertyName);
            }
        }
        private void ReadCumulus()
        {
            cumulusHealthy = false;
            try
            {
                string path = DriverSettings.CumulusFile;

                tl?.LogMessage(
                    "ReadCumulus",
                    "Path = " + path);

                if (string.IsNullOrWhiteSpace(path))
                {
                    tl?.LogMessage(
                        "ReadCumulus",
                        "Path is blank");

                    return;
                }

                if (!System.IO.File.Exists(path))
                {
                    tl?.LogMessage(
                        "ReadCumulus",
                        "File does not exist");

                    return;
                }

                string line =
                    System.IO.File.ReadAllText(path).Trim();

                tl?.LogMessage(
                    "ReadCumulus",
                    "Raw line = " + line);

                string[] p =
                    line.Split(new char[] { ' ' },
                    StringSplitOptions.RemoveEmptyEntries);

                if (p.Length < 11)
                {
                    tl?.LogMessage(
                        "ReadCumulus",
                        "Invalid file format");

                    return;
                }

                //
                // Correct field mapping
                //

                Data.Temperature = ParseDouble(p, 2);   // 28.4
                Data.Humidity = ParseDouble(p, 3);   // 84
                Data.DewPoint = ParseDouble(p, 5);   // 24.2
                Data.WindSpeed = ParseDouble(p, 5);   // avg wind speed
                Data.WindGust = ParseDouble(p, 6);   // latest wind speed
                Data.WindDirection = ParseDouble(p, 7);   // 261
                Data.RainRate = ParseDouble(p, 8);   // 0.0
                Data.Pressure = ParseDouble(p, 10);  // 999.7
                cumulusHealthy = !double.IsNaN(Data.Temperature) && !double.IsNaN(Data.Humidity) && !double.IsNaN(Data.WindSpeed) && !double.IsNaN(Data.RainRate);

                tl?.LogMessage(
                    "ReadCumulus",
                    $"Temp={Data.Temperature}, " +
                    $"Humidity={Data.Humidity}, " +
                    $"DewPoint={Data.DewPoint}, " +
                    $"WindSpeed={Data.WindSpeed}, " +
                    $"WindGust={Data.WindGust}, " +
                    $"WindDir={Data.WindDirection}, " +
                    $"RainRate={Data.RainRate}, " +
                    $"Pressure={Data.Pressure}");
            }
            catch (Exception ex)
            {
                tl?.LogMessageCrLf(
                    "ReadCumulus",
                    ex.ToString());
            }
        }

        private void ReadBoltwood()
        {
            try
            {
                string path = DriverSettings.BoltwoodFile;

                tl?.LogMessage(
                    "ReadBoltwood",
                    "Path = " + path);

                if (string.IsNullOrWhiteSpace(path))
                    return;

                if (!System.IO.File.Exists(path))
                    return;

                string line =
                    System.IO.File.ReadAllText(path).Trim();

                string[] p = line.Split(
                    new char[] { ' ', '\t' },
                    StringSplitOptions.RemoveEmptyEntries);

                if (p.Length < 13)
                    return;

                //
                // Boltwood astronomy fields
                //

                Data.SkyTemperature = ParseDouble(p, 4);

                if (!DriverSettings.UseAmbient && Data.Temperature == 0)
                {
                    Data.Temperature = ParseDouble(p, 5);

                    tl?.LogMessage(
                        "ReadBoltwood",
                        "Temperature fallback from Boltwood = " +
                        Data.Temperature);
                }

                bool rainDetected =
                    p[11] != "0";

                bool wetDetected =
                    p[12] != "0";

                Data.RainUnsafe = DriverSettings.UseAmbient
                    ? Data.RainUnsafe || rainDetected || wetDetected
                    : rainDetected || wetDetected;
                if (!DriverSettings.UseAmbient)
                    Data.RainRate = Data.RainUnsafe ? 1 : 0;
                boltwoodUpdateUtc = System.IO.File.GetLastWriteTimeUtc(path);

                //
                // Derived cloud cover from sky temp
                //

                if (Data.SkyTemperature < -20)
                    Data.CloudCover = 0;
                else if (Data.SkyTemperature < -10)
                    Data.CloudCover = 25;
                else if (Data.SkyTemperature < -5)
                    Data.CloudCover = 50;
                else if (Data.SkyTemperature < 0)
                    Data.CloudCover = 75;
                else
                    Data.CloudCover = 100;

                tl?.LogMessage(
                    "ReadBoltwood",
                    $"SkyTemp={Data.SkyTemperature}, " +
                    $"CloudCover={Data.CloudCover}, " +
                    $"RainUnsafe={Data.RainUnsafe}");
            }
            catch (Exception ex)
            {
                tl?.LogMessageCrLf(
                    "ReadBoltwood",
                    ex.ToString());
            }
        }

        private void WriteSafetyFile()
        {
            tl?.LogMessage(
                "WriteSafetyFile",
                "Method entered");

            try
            {
                string folder =
                    @"C:\ProgramData\WeatherWatcher2";

                tl?.LogMessage(
                    "WriteSafetyFile",
                    "Folder = " + folder);

                if (!System.IO.Directory.Exists(folder))
                {
                    System.IO.Directory.CreateDirectory(folder);

                    tl?.LogMessage(
                        "WriteSafetyFile",
                        "Folder created");
                }

                string file =
                    System.IO.Path.Combine(
                        folder,
                        "weatherdata.txt");

                string value =
                    Data.IsSafe ? "0" : "1";

                tl?.LogMessage(
                    "WriteSafetyFile",
                    "About to write value = " + value);

                if (DriverSettings.UseRainCloud) value = "WW2RC1|" + DateTime.UtcNow.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + value;
                string temporary = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try { System.IO.File.WriteAllText(temporary, value); if (File.Exists(file)) File.Replace(temporary, file, null); else File.Move(temporary, file); }
                finally { if (File.Exists(temporary)) File.Delete(temporary); }

                tl?.LogMessage(
                    "WriteSafetyFile",
                    "SUCCESS wrote file = " + file);
            }
            catch (Exception ex)
            {
                tl?.LogMessageCrLf(
                    "WriteSafetyFile",
                    ex.ToString());
            }
        }
        private bool IsFileStale(string path, int maxAgeSeconds)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    return true;

                if (!System.IO.File.Exists(path))
                    return true;

                DateTime lastWrite =
                    System.IO.File.GetLastWriteTime(path);

                TimeSpan age =
                    DateTime.Now - lastWrite;

                tl?.LogMessage(
                    "IsFileStale",
                    $"File={path}, Age={age.TotalSeconds:F0}s");

                return age.TotalSeconds > maxAgeSeconds;
            }
            catch (Exception ex)
            {
                tl?.LogMessageCrLf(
                    "IsFileStale",
                    ex.ToString());

                return true;
            }
        }
        private void EvaluateSafeState()
        {
            double maxWind = DriverSettings.MaxWind;
            double maxHumidity = DriverSettings.MaxHumidity;
            double minTemp = DriverSettings.MinTemp;

            Data.WindUnsafe = Data.WindSpeed > maxWind;
            Data.HumidityUnsafe = Data.Humidity > maxHumidity;
            Data.TemperatureUnsafe = Data.Temperature < minTemp;

            Data.IsSafe =
                !(Data.RainUnsafe ||
                  Data.WindUnsafe ||
                  Data.HumidityUnsafe ||
                  Data.TemperatureUnsafe ||
                  Data.CloudUnsafe ||
                  Data.WetUnsafe);
            if (DriverSettings.UseAmbient)
                Data.IsSafe = Data.IsSafe && ambientHealthy &&
                    (DriverSettings.UseRainCloud || !DriverSettings.UseBoltwood ||
                     (boltwoodUpdateUtc != DateTime.MinValue &&
                      !IsFileStale(DriverSettings.BoltwoodFile, DriverSettings.AmbientMaxAgeSeconds)));
        }

        public string SensorDescription(string propertyName)
        {
            if (DriverSettings.UseRainCloud && propertyName == "SkyTemperature") throw new ASCOM.MethodNotImplementedException("SensorDescription(SkyTemperature)");
            if (DriverSettings.UseRainCloud && propertyName == "CloudCover") return "NOAA GOES-East ABI Level-2 clear sky mask: percent good-quality cloudy/probably cloudy pixels within configured ground radius; informational only";
            return propertyName;
        }

        public double TimeSinceLastUpdate(string propertyName)
        {
            lock (gate)
            {
                if (DriverSettings.UseRainCloud && propertyName == "SkyTemperature") throw new ASCOM.MethodNotImplementedException("TimeSinceLastUpdate(SkyTemperature)");
                if (DriverSettings.UseRainCloud && propertyName == "CloudCover")
                {
                    ReadValue("CloudCover");
                    return (DateTime.UtcNow - cloudReading.ObservationUtc).TotalSeconds;
                }
                if (!DriverSettings.UseAmbient)
                    return (DateTime.UtcNow - Data.LastUpdateUtc).TotalSeconds;
                Refresh();
                bool sky = propertyName == "SkyTemperature" || propertyName == "CloudCover";
                if (sky && !DriverSettings.UseBoltwood)
                    throw new ASCOM.PropertyNotImplementedException(propertyName, false);
                DateTime timestamp = sky ? boltwoodUpdateUtc : ambientUpdateUtc;
                if (propertyName == "" && boltwoodUpdateUtc > timestamp) timestamp = boltwoodUpdateUtc;
                if (timestamp == DateTime.MinValue)
                    throw new ASCOM.DriverException("No weather observation has been received.");
                return Math.Max(0, (DateTime.UtcNow - timestamp).TotalSeconds);
            }
        }

        public double Value(string propertyName)
        {
            switch (propertyName)
            {
                case "Temperature":
                    return Data.Temperature;

                case "Humidity":
                    return Data.Humidity;

                case "DewPoint":
                    return Data.DewPoint;

                case "Pressure":
                    return Data.Pressure;

                case "CloudCover":
                    return Data.CloudCover;

                case "RainRate":
                    return Data.RainRate;

                case "SkyTemperature":
                    return Data.SkyTemperature;

                case "WindSpeed":
                    return Data.WindSpeed;

                case "WindGust":
                    return Data.WindGust;

                case "WindDirection":
                    return Data.WindDirection;

                default:
                    return 0;
            }
        }

        private double ParseDouble(string[] p, int index)
        {
            if (index >= p.Length)
                return 0;

            double value;
            if (DriverSettings.UseRainCloud) return double.TryParse(p[index], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value) && !double.IsInfinity(value) ? value : double.NaN;
            if (!double.TryParse(p[index], out value))
                value = 0;

            return value;
        }
    }
}
