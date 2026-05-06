using ASCOM.Utilities;

namespace WeatherWatcher2.ObservingConditions
{
    public static class DriverSettings
    {
        public static string BoltwoodFile = "";
        public static string CumulusFile = "";

        public static double MaxWind = 20;
        public static double MaxHumidity = 90;
        public static double MinTemp = 0;
        public static double MaxTemp = 35;

        public static bool UseBoltwood = true;
        public static bool UseCumulus = true;
        public static bool EnableLogging = true;

        public static void Load()
        {
            using (Profile profile = new Profile())
            {
                profile.DeviceType = "ObservingConditions";

                BoltwoodFile = profile.GetValue(
                    "WeatherWatcher2.ObservingConditions",
                    "BoltwoodFile",
                    "",
                    BoltwoodFile);

                CumulusFile = profile.GetValue(
                    "WeatherWatcher2.ObservingConditions",
                    "CumulusFile",
                    "",
                    CumulusFile);
            }
        }

        public static void Save()
        {
            using (Profile profile = new Profile())
            {
                profile.DeviceType = "ObservingConditions";

                profile.WriteValue(
                    "WeatherWatcher2.ObservingConditions",
                    "BoltwoodFile",
                    BoltwoodFile);

                profile.WriteValue(
                    "WeatherWatcher2.ObservingConditions",
                    "CumulusFile",
                    CumulusFile);
            }
        }
    }
}