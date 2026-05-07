using ASCOM.Utilities;
using System;
using System.IO;

namespace WeatherWatcher2.ObservingConditions
{
    public class WeatherDataReader
    {
        private readonly TraceLogger tl;

        public WeatherSnapshot Data { get; private set; }

        public WeatherDataReader(TraceLogger logger)
        {
            tl = logger;
            Data = new WeatherSnapshot();
        }

        public void Refresh()
        {
            ReadCumulus();
            ReadBoltwood();
            EvaluateSafeState();
            WriteSafetyFile();

            Data.LastUpdateUtc =
                DateTime.UtcNow;
        }

        private void ReadCumulus()
        {
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

                if (Data.Temperature == 0)
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

                Data.RainUnsafe =
                    rainDetected || wetDetected;

                Data.RainRate =
                    Data.RainUnsafe ? 1 : 0;

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

                System.IO.File.WriteAllText(file, value);

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
            double maxWind = ProfileManager.ReadDouble("MaxWind", 20);
            double maxHumidity = ProfileManager.ReadDouble("MaxHumidity", 90);
            double minTemp = ProfileManager.ReadDouble("MinTemp", 0);

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
        }

        public string SensorDescription(string propertyName)
        {
            return propertyName;
        }

        public double TimeSinceLastUpdate(string propertyName)
        {
            return (DateTime.UtcNow - Data.LastUpdateUtc).TotalSeconds;
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

            if (!double.TryParse(p[index], out value))
                value = 0;

            return value;
        }
    }
}