using ASCOM.Utilities;
using System;

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
            try
            {
                using (Profile profile = new Profile())
                {
                    profile.DeviceType = "ObservingConditions";

                    BoltwoodFile = profile.GetValue(
                        "WeatherWatcher2.ObservingConditions",
                        "BoltwoodFile",
                        "",
                        @"C:\ProgramData\WeatherWatcher2\weatherdata.txt");

                    CumulusFile = profile.GetValue(
                        "WeatherWatcher2.ObservingConditions",
                        "CumulusFile",
                        "",
                        @"C:\Cumulus\realtime.txt");

                    EnableLogging = Convert.ToBoolean(
                        profile.GetValue(
                            "WeatherWatcher2.ObservingConditions",
                            "EnableLogging",
                            "",
                            "false"));
                }
            }
            catch (Exception)
            {
                // First install / ASCOM profile not registered yet
                // Fall back to defaults so constructor does not fail

                BoltwoodFile =
                    @"C:\ProgramData\WeatherWatcher2\weatherdata.txt";

                CumulusFile =
                    @"C:\Cumulus\realtime.txt";

                EnableLogging = false;
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