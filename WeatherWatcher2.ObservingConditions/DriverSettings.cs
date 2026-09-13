using ASCOM.Utilities;
using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace WeatherWatcher2.ObservingConditions
{
    public static class DriverSettings
    {
        private const string DriverId = "WeatherWatcher2.ObservingConditions";
        public static string BoltwoodFile = @"C:\ProgramData\WeatherWatcher2\weatherdata.txt";
        public static string CumulusFile = @"C:\Cumulus\realtime.txt";
        public static double MaxWind = 20, MaxHumidity = 90, MinTemp = 0, MaxTemp = 35;
        public static bool UseBoltwood = true, UseCumulus = true, EnableLogging = false;
        public static bool UseRainCloud = false;
        public static string RainCloudPort = "COM5";
        public static double RainCloudMaxCloud = 30;
        public static int RainCloudRecoverySeconds = 300;
        public static bool UseAmbient = false;
        public static string AmbientApplicationKey = "", AmbientApiKey = "", AmbientMacAddress = "";
        public static int AmbientMaxAgeSeconds = 300;

        public static void Load()
        {
            try
            {
                using (Profile p = new Profile())
                {
                    p.DeviceType = "ObservingConditions";
                    BoltwoodFile = p.GetValue(DriverId, "BoltwoodFile", "", BoltwoodFile);
                    CumulusFile = p.GetValue(DriverId, "CumulusFile", "", CumulusFile);
                    UseBoltwood = ReadBool(p, "UseBoltwood", true);
                    UseCumulus = ReadBool(p, "UseCumulus", true);
                    UseAmbient = ReadBool(p, "UseAmbient", false);
                    UseRainCloud = ReadBool(p, "UseRainCloud", false);
                    RainCloudPort = p.GetValue(DriverId, "RainCloudPort", "", "COM5");
                    double cloudLimit;
                    RainCloudMaxCloud = double.TryParse(p.GetValue(DriverId, "RainCloudMaxCloud", "", "30"), NumberStyles.Float, CultureInfo.InvariantCulture, out cloudLimit) && !double.IsNaN(cloudLimit) && cloudLimit >= 0 && cloudLimit <= 100 ? cloudLimit : 30;
                    RainCloudRecoverySeconds = 300;
                    EnableLogging = ReadBool(p, "EnableLogging", false);
                    AmbientApplicationKey = Unprotect(p.GetValue(DriverId, "AmbientApplicationKeyProtected", "", ""));
                    AmbientApiKey = Unprotect(p.GetValue(DriverId, "AmbientApiKeyProtected", "", ""));
                    AmbientMacAddress = p.GetValue(DriverId, "AmbientMacAddress", "", "");
                    int age;
                    AmbientMaxAgeSeconds = int.TryParse(p.GetValue(DriverId, "AmbientMaxAgeSeconds", "", "300"), out age)
                        && age >= 60 && age <= 3600 ? age : 300;
                    MaxWind = ProfileManager.ReadDouble("MaxWind", 20);
                    MaxHumidity = ProfileManager.ReadDouble("MaxHumidity", 90);
                    MinTemp = ProfileManager.ReadDouble("MinTemp", 0);
                    MaxTemp = ProfileManager.ReadDouble("MaxTemp", 35);
                }
            }
            catch
            {
                // Preserve first-install behavior when the ASCOM profile is not yet available.
                // Do not switch an already selected Ambient source to Cumulus on a read failure.
                EnableLogging = false;
            }
        }
        private static bool ReadBool(Profile p, string name, bool fallback)
        {
            bool value;
            return bool.TryParse(p.GetValue(DriverId, name, "", fallback.ToString()), out value) ? value : fallback;
        }

        public static void Save()
        {
            // Encrypt before writing any settings, so an encryption failure cannot enable a half-configured source.
            string appKey = Protect(AmbientApplicationKey);
            string apiKey = Protect(AmbientApiKey);
            using (Profile p = new Profile())
            {
                p.DeviceType = "ObservingConditions";
                p.WriteValue(DriverId, "BoltwoodFile", BoltwoodFile);
                p.WriteValue(DriverId, "CumulusFile", CumulusFile);
                p.WriteValue(DriverId, "UseBoltwood", UseBoltwood.ToString());
                p.WriteValue(DriverId, "UseCumulus", UseCumulus.ToString());
                p.WriteValue(DriverId, "EnableLogging", EnableLogging.ToString());
                p.WriteValue(DriverId, "AmbientApplicationKeyProtected", appKey);
                p.WriteValue(DriverId, "AmbientApiKeyProtected", apiKey);
                p.WriteValue(DriverId, "AmbientMacAddress", AmbientMacAddress);
                p.WriteValue(DriverId, "AmbientMaxAgeSeconds", AmbientMaxAgeSeconds.ToString(CultureInfo.InvariantCulture));
                p.WriteValue(DriverId, "UseAmbient", UseAmbient.ToString());
                p.WriteValue(DriverId, "UseRainCloud", UseRainCloud.ToString());
                p.WriteValue(DriverId, "RainCloudPort", RainCloudPort);
                p.WriteValue(DriverId, "RainCloudMaxCloud", RainCloudMaxCloud.ToString(CultureInfo.InvariantCulture));
                p.WriteValue(DriverId, "MaxWind", MaxWind.ToString());
                p.WriteValue(DriverId, "MaxHumidity", MaxHumidity.ToString());
                p.WriteValue(DriverId, "MinTemp", MinTemp.ToString());
                p.WriteValue(DriverId, "MaxTemp", MaxTemp.ToString());
            }
        }

        private static string Protect(string value)
        {
            return string.IsNullOrEmpty(value) ? "" : Convert.ToBase64String(ProtectedData.Protect(
                Encoding.UTF8.GetBytes(value), null, DataProtectionScope.CurrentUser));
        }

        private static string Unprotect(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            try
            {
                return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(value),
                    null, DataProtectionScope.CurrentUser));
            }
            catch (CryptographicException) { return ""; }
            catch (FormatException) { return ""; }
        }
    }
}
