using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using ASCOM.Utilities;

namespace WeatherWatcher2.ObservingConditions
{
    internal sealed class PushoverSettings
    {
        internal bool Enabled, NotifyClearing = true, NotifyAvailability;
        internal string Token = "", User = "", Device = "";
        internal int WarnPercent = 30, ClearPercent = 20, CooldownMinutes = 30;
        internal PushoverSettings Copy() { return (PushoverSettings)MemberwiseClone(); }
        internal string ValidationError()
        {
            if (!Regex.IsMatch(Token ?? "", "^[A-Za-z0-9]{30}$") || !Regex.IsMatch(User ?? "", "^[A-Za-z0-9]{30}$"))
                return "Enter the 30-character Pushover application token and user/group key.";
            if (!Regex.IsMatch(Device ?? "", "^[A-Za-z0-9_-]{0,25}$")) return "Device must be blank or a Pushover device name (up to 25 letters, numbers, underscores or hyphens).";
            if (ClearPercent < 0 || WarnPercent > 100 || ClearPercent >= WarnPercent) return "The clearing threshold must be below the warning threshold (0–100%).";
            if (CooldownMinutes < 1 || CooldownMinutes > 1440) return "Cooldown must be 1–1440 minutes.";
            return null;
        }
        // Used only for in-memory change detection. Never log this key or credentials.
        internal string Key => string.Join("|", Enabled, Token, User, Device, WarnPercent, ClearPercent, CooldownMinutes, NotifyClearing, NotifyAvailability);
    }

    internal sealed class CloudNotice
    {
        internal string Title, Message;
        internal bool Availability, Value;
    }

    internal sealed class CloudAlertState
    {
        private bool? cloudy;
        private bool deliveredCloudy, deliveredUnavailable, everValid;
        private DateTime? noDataSince;
        private DateTime lastObservation = DateTime.MinValue;
        internal CloudNotice Evaluate(CloudReading reading, DateTime now, int staleSeconds, double radius, PushoverSettings settings)
        {
            bool fresh = reading != null && reading.IsFresh(now, staleSeconds) && reading.ObservationUtc >= lastObservation;
            if (fresh)
            {
                everValid = true; noDataSince = null;
                if (reading.ObservationUtc > lastObservation)
                {
                    lastObservation = reading.ObservationUtc;
                    if (reading.Percent >= settings.WarnPercent) cloudy = true;
                    else if (reading.Percent <= settings.ClearPercent) cloudy = false;
                }
            }
            else if (!noDataSince.HasValue) noDataSince = now;
            // Allow initial NOAA acquisition a full stale-timeout window before reporting no data.
            bool unavailable = !fresh && (everValid || (now - noDataSince.Value).TotalSeconds >= staleSeconds);
            string details = fresh ? string.Format(CultureInfo.InvariantCulture,
                "Cloud cover: {0:F1}% within {1:F0} km. NOAA observation: {2:yyyy-MM-dd HH:mm} UTC ({3:F0} minutes old). ",
                reading.Percent, radius, reading.ObservationUtc, (now - reading.ObservationUtc).TotalMinutes) : "NOAA cloud data is unavailable or stale. ";
            if (settings.NotifyAvailability && unavailable != deliveredUnavailable && (unavailable || fresh))
                return new CloudNotice { Availability = true, Value = unavailable,
                    Title = unavailable ? "WeatherWatcher — Cloud data unavailable" : "WeatherWatcher — Cloud data restored",
                    Message = details + "Informational only; safety status unchanged." };
            if (!fresh || !cloudy.HasValue || cloudy.Value == deliveredCloudy) return null;
            if (!cloudy.Value && !settings.NotifyClearing) { deliveredCloudy = false; return null; }
            return new CloudNotice { Value = cloudy.Value,
                Title = cloudy.Value ? "WeatherWatcher — Cloud warning" : "WeatherWatcher — Clouds clearing",
                Message = details + "Informational only; safety status unchanged." };
        }
        internal void Delivered(CloudNotice notice)
        {
            if (notice.Availability) deliveredUnavailable = notice.Value;
            else deliveredCloudy = notice.Value;
        }
    }

    // One notifier per local-server process, shared by all ASCOM clients. No safety state access.
    internal sealed class CloudNotifier
    {
        internal static readonly CloudNotifier Shared = new CloudNotifier();
        private readonly object gate = new object();
        private readonly Func<PushoverSettings, CloudNotice, Task> send;
        private CloudAlertState state = new CloudAlertState();
        private string key;
        private DateTime nextAttempt = DateTime.MinValue;
        private bool busy;
        internal bool Busy { get { lock (gate) return busy; } }
        internal CloudNotifier(Func<PushoverSettings, CloudNotice, Task> send = null) { this.send = send ?? PushoverClient.Send; }
        internal void Process(CloudReading reading, DateTime now, int staleSeconds, NoaaSite site, PushoverSettings settings)
        {
            lock (gate)
            {
                string selection = settings.Key + "|" + site.Key + "|" + staleSeconds;
                if (selection != key) { key = selection; state = new CloudAlertState(); nextAttempt = DateTime.MinValue; }
                if (!settings.Enabled || settings.ValidationError() != null || !site.Valid) return;
                var notice = state.Evaluate(reading, now, staleSeconds, site.RadiusKm, settings);
                if (notice == null || busy || now < nextAttempt) return;
                busy = true;
                nextAttempt = now.AddMinutes(settings.CooldownMinutes);
                var sendingState = state;
                var credentials = settings.Copy();
                Task.Run(async () =>
                {
                    try
                    {
                        lock (gate) { if (!ReferenceEquals(state, sendingState)) return; }
                        await send(credentials, notice).ConfigureAwait(false);
                        lock (gate) { if (ReferenceEquals(state, sendingState)) state.Delivered(notice); }
                        Log("Pushover accepted: " + notice.Title);
                    }
                    catch { Log("Pushover delivery failed. Check connectivity, credentials and quota; retry is limited by the configured cooldown. Safety is unchanged."); }
                    finally { lock (gate) busy = false; }
                });
            }
        }
        private static void Log(string text)
        {
            if (!DriverSettings.EnableLogging) return;
            try { using (var log = new TraceLogger("", "WeatherWatcher2.Pushover")) { log.Enabled = true; log.LogMessage("Cloud notification", text); } }
            catch { }
        }
    }

    internal static class PushoverClient
    {
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(15), MaxResponseContentBufferSize = 65536 };
        internal static Task Send(PushoverSettings settings, CloudNotice notice) { return SendWith(Http, settings, notice); }
        internal static async Task SendWith(HttpClient client, PushoverSettings settings, CloudNotice notice)
        {
            using (var content = new FormUrlEncodedContent(new Dictionary<string, string> {
                {"token", settings.Token}, {"user", settings.User}, {"device", settings.Device},
                {"title", notice.Title}, {"message", notice.Message}, {"priority", "0"}, {"ttl", "1800"}
            }))
            using (var response = await client.PostAsync("https://api.pushover.net/1/messages.json", content).ConfigureAwait(false))
            {
                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                ValidateResponse(response.IsSuccessStatusCode, json);
            }
        }
        internal static void ValidateResponse(bool success, string json)
        {
            // Do not echo server responses: credentials and private device information must not reach logs/UI.
            object status;
            var result = new JavaScriptSerializer { MaxJsonLength = 65536 }.Deserialize<Dictionary<string, object>>(json);
            if (!success || result == null || !result.TryGetValue("status", out status) || !(status is int) || (int)status != 1)
                throw new InvalidOperationException("Pushover did not accept the notification.");
        }
    }
}
