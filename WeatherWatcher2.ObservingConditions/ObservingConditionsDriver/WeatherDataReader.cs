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
            Data.LastUpdateUtc = DateTime.UtcNow;
        }

        private void ReadCumulus()
        {
            string path = ProfileManager.Read("CumulusPath", "");

            if (!File.Exists(path))
                return;

            string[] p = File.ReadAllText(path).Split(' ');

            Data.Temperature = ParseDouble(p, 2);
            Data.Humidity = ParseDouble(p, 5);
            Data.DewPoint = ParseDouble(p, 6);
            Data.WindDirection = ParseDouble(p, 9);
            Data.WindSpeed = ParseDouble(p, 10);
            Data.WindGust = ParseDouble(p, 12);
            Data.Pressure = ParseDouble(p, 57);
        }

        private void ReadBoltwood()
        {
            string path = ProfileManager.Read("BoltwoodPath", "");

            if (!File.Exists(path))
                return;

            string[] p = File.ReadAllText(path).Split(',');

            Data.SkyTemperature = ParseDouble(p, 4);
            Data.CloudCover = ParseDouble(p, 5);
            Data.RainRate = ParseDouble(p, 6);

            if (p.Length > 7 && p[7].ToUpper().Contains("RAIN"))
            {
                Data.RainUnsafe = true;
                Data.RainRate = 1;
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