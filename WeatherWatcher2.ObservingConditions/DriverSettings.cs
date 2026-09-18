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
        public static int RainCloudRecoverySeconds = 300;
        public static double ObservatoryLatitude = double.NaN, ObservatoryLongitude = double.NaN;
        public static double NoaaRadiusKm = 15;
        public static int NoaaPollSeconds = 600, NoaaStaleSeconds = 1800;
        internal static PushoverSettings Pushover = new PushoverSettings();
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
                    RainCloudRecoverySeconds = (int)ReadNumber(p, "RainCloudRecoverySeconds", 300, 0, 3600);
                    ObservatoryLatitude = ReadNumber(p, "ObservatoryLatitude", double.NaN, -90, 90);
                    ObservatoryLongitude = ReadNumber(p, "ObservatoryLongitude", double.NaN, -180, 180);
                    NoaaRadiusKm = ReadNumber(p, "NoaaRadiusKm", 15, 5, 100);
                    NoaaPollSeconds = (int)ReadNumber(p, "NoaaPollSeconds", 600, 600, 3600);
                    NoaaStaleSeconds = (int)ReadNumber(p, "NoaaStaleSeconds", 1800, 600, 86400);
                    Pushover = new PushoverSettings {
                        Enabled = ReadBool(p, "PushoverEnabled", false),
                        NotifyClearing = ReadBool(p, "PushoverNotifyClearing", true),
                        NotifyAvailability = ReadBool(p, "PushoverNotifyAvailability", false),
                        Token = Unprotect(p.GetValue(DriverId, "PushoverTokenProtected", "", "")),
                        User = Unprotect(p.GetValue(DriverId, "PushoverUserProtected", "", "")),
                        Device = p.GetValue(DriverId, "PushoverDevice", "", ""),
                        WarnPercent = (int)ReadNumber(p, "PushoverWarnPercent", 30, 1, 100),
                        ClearPercent = (int)ReadNumber(p, "PushoverClearPercent", 20, 0, 99),
                        CooldownMinutes = (int)ReadNumber(p, "PushoverCooldownMinutes", 30, 1, 1440)
                    };
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
        private static double ReadNumber(Profile p, string name, double fallback, double minimum, double maximum)
        {
            double value;
            return double.TryParse(p.GetValue(DriverId, name, "", fallback.ToString(CultureInfo.InvariantCulture)), NumberStyles.Float,
                CultureInfo.InvariantCulture, out value) && value >= minimum && value <= maximum ? value : fallback;
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
            string pushToken = Protect(Pushover.Token), pushUser = Protect(Pushover.User);
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
                p.WriteValue(DriverId, "PushoverTokenProtected", pushToken);
                p.WriteValue(DriverId, "PushoverUserProtected", pushUser);
                p.WriteValue(DriverId, "PushoverDevice", Pushover.Device);
                p.WriteValue(DriverId, "PushoverEnabled", Pushover.Enabled.ToString());
                p.WriteValue(DriverId, "PushoverNotifyClearing", Pushover.NotifyClearing.ToString());
                p.WriteValue(DriverId, "PushoverNotifyAvailability", Pushover.NotifyAvailability.ToString());
                p.WriteValue(DriverId, "PushoverWarnPercent", Pushover.WarnPercent.ToString(CultureInfo.InvariantCulture));
                p.WriteValue(DriverId, "PushoverClearPercent", Pushover.ClearPercent.ToString(CultureInfo.InvariantCulture));
                p.WriteValue(DriverId, "PushoverCooldownMinutes", Pushover.CooldownMinutes.ToString(CultureInfo.InvariantCulture));
                p.WriteValue(DriverId, "UseRainCloud", UseRainCloud.ToString());
                p.WriteValue(DriverId, "RainCloudPort", RainCloudPort);
                p.WriteValue(DriverId, "RainCloudRecoverySeconds", RainCloudRecoverySeconds.ToString(CultureInfo.InvariantCulture));
                p.WriteValue(DriverId, "ObservatoryLatitude", ObservatoryLatitude.ToString(CultureInfo.InvariantCulture));
                p.WriteValue(DriverId, "ObservatoryLongitude", ObservatoryLongitude.ToString(CultureInfo.InvariantCulture));
                p.WriteValue(DriverId, "NoaaRadiusKm", NoaaRadiusKm.ToString(CultureInfo.InvariantCulture));
                p.WriteValue(DriverId, "NoaaPollSeconds", NoaaPollSeconds.ToString(CultureInfo.InvariantCulture));
                p.WriteValue(DriverId, "NoaaStaleSeconds", NoaaStaleSeconds.ToString(CultureInfo.InvariantCulture));
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
