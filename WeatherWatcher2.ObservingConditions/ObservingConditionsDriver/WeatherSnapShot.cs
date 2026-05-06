using System;

namespace WeatherWatcher2.ObservingConditions
{
    public class WeatherSnapshot
    {
        public double Temperature { get; set; }
        public double DewPoint { get; set; }
        public double Humidity { get; set; }
        public double Pressure { get; set; }
        public double WindSpeed { get; set; }
        public double WindGust { get; set; }
        public double WindDirection { get; set; }
        public double SkyTemperature { get; set; }
        public double CloudCover { get; set; }
        public double RainRate { get; set; }

        public bool RainUnsafe { get; set; }
        public bool WetUnsafe { get; set; }
        public bool CloudUnsafe { get; set; }
        public bool WindUnsafe { get; set; }
        public bool HumidityUnsafe { get; set; }
        public bool TemperatureUnsafe { get; set; }
        public bool IsSafe { get; set; } = true;

        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;
    }
}