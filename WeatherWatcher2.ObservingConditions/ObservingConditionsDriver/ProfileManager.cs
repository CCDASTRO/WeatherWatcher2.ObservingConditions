using ASCOM.Utilities;

namespace WeatherWatcher2.ObservingConditions
{
    public static class ProfileManager
    {
        private const string DriverId = "WeatherWatcher2.ObservingConditions";
        private const string DeviceType = "ObservingConditions";

        public static string Read(string key, string defaultValue)
        {
            using (Profile p = new Profile())
            {
                p.DeviceType = DeviceType;
                return p.GetValue(DriverId, key, string.Empty, defaultValue);
            }
        }

        public static void Write(string key, string value)
        {
            using (Profile p = new Profile())
            {
                p.DeviceType = DeviceType;
                p.WriteValue(DriverId, key, value);
            }
        }

        public static double ReadDouble(string key, double defaultValue)
        {
            double value;
            if (!double.TryParse(Read(key, defaultValue.ToString()), out value))
                value = defaultValue;
            return value;
        }

        public static bool ReadBool(string key, bool defaultValue)
        {
            bool value;
            if (!bool.TryParse(Read(key, defaultValue.ToString()), out value))
                value = defaultValue;
            return value;
        }
    }
}