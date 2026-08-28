using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace WeatherWatcher2.ObservingConditions
{
    // Shared across ASCOM driver instances so property reads do not multiply API requests.
    internal sealed class AmbientWeatherClient
    {
        internal static readonly AmbientWeatherClient Shared = new AmbientWeatherClient();
        private static readonly HttpClient Http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        private readonly object gate = new object();
        private string identity;
        private DateTime nextRequestUtc;
        private Task pending;
        private AmbientReading reading;
        private string error = "Waiting for Ambient Weather data.";
        internal Func<string, string, Task<string>> Download = DownloadAsync;
        internal Func<DateTime> UtcNow = () => DateTime.UtcNow;

        internal AmbientReading Get(string applicationKey, string apiKey, string mac, out string failure)
        {
            lock (gate)
            {
                string key = applicationKey + "\n" + apiKey + "\n" + mac;
                if (identity != key)
                {
                    identity = key;
                    reading = null;
                    error = "Waiting for Ambient Weather data.";
                    // Preserve the cooldown even when settings change.
                }
                if ((pending == null || pending.IsCompleted) && UtcNow() >= nextRequestUtc)
                {
                    nextRequestUtc = UtcNow().AddSeconds(60);
                    pending = Task.Run(async () =>
                    {
                        AmbientReading result = null;
                        string problem = null;
                        try
                        {
                            if (string.IsNullOrWhiteSpace(applicationKey) || string.IsNullOrWhiteSpace(apiKey) ||
                                string.IsNullOrWhiteSpace(mac))
                                throw new AmbientException("Configure both Ambient keys and the station MAC address.");
                            result = Parse(await Download(applicationKey, apiKey).ConfigureAwait(false), mac, UtcNow());
                        }
                        catch (Exception ex)
                        {
                            // Network exceptions can contain the request URL and its credentials.
                            problem = ex is AmbientException ? ex.Message : "Ambient request failed. Check credentials, network and station configuration.";
                        }
                        lock (gate)
                        {
                            if (identity != key) return;
                            if (result != null) reading = result;
                            error = problem;
                        }
                    });
                }
                failure = error;
                return reading;
            }
        }

        private static async Task<string> DownloadAsync(string applicationKey, string apiKey)
        {
            string url = "https://rt.ambientweather.net/v1/devices?applicationKey=" +
                Uri.EscapeDataString(applicationKey) + "&apiKey=" + Uri.EscapeDataString(apiKey);
            using (HttpResponseMessage response = await Http.GetAsync(url).ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode)
                    throw new AmbientException("Ambient API returned HTTP " + (int)response.StatusCode +
                        ". Requests are limited to once per minute; check keys if access is denied.");
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }

        internal static AmbientReading Parse(string json, string mac, DateTime now)
        {
            var serializer = new DataContractJsonSerializer(typeof(List<AmbientDevice>));
            List<AmbientDevice> devices;
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                devices = (List<AmbientDevice>)serializer.ReadObject(stream);
            AmbientData data = null;
            foreach (var device in devices ?? new List<AmbientDevice>())
                if (device != null && string.Equals(device.MacAddress, mac.Trim(), StringComparison.OrdinalIgnoreCase))
                    data = device.LastData;
            if (data == null || !data.DateUtc.HasValue)
                throw new AmbientException("Selected station has no timestamped data. Check its MAC address and upload status.");
            DateTime timestamp = DateTimeOffset.FromUnixTimeMilliseconds(data.DateUtc.Value).UtcDateTime;
            if (timestamp > now.AddMinutes(1))
                throw new AmbientException("Ambient station timestamp is in the future.");
            return new AmbientReading
            {
                TimestampUtc = timestamp,
                Temperature = ConvertValue(data.TemperatureF, v => (v - 32) / 1.8),
                DewPoint = ConvertValue(data.DewPointF, v => (v - 32) / 1.8),
                Humidity = Range(data.Humidity, 0, 100),
                Pressure = ConvertValue(Positive(data.AbsolutePressure), v => v * 33.8638866667),
                WindSpeed = ConvertValue(Positive(data.WindSpeed), v => v * 0.44704),
                WindGust = ConvertValue(Positive(data.WindGust), v => v * 0.44704),
                WindDirection = Range(data.WindDirection, 0, 360),
                RainRate = ConvertValue(Positive(data.HourlyRain), v => v * 25.4)
            };
        }

        private static double? Positive(double? value) { return value >= 0 ? value : null; }
        private static double Range(double? value, double min, double max)
        {
            return value >= min && value <= max ? value.Value : double.NaN;
        }
        private static double ConvertValue(double? value, Func<double, double> convert)
        {
            if (!value.HasValue || double.IsNaN(value.Value) || double.IsInfinity(value.Value)) return double.NaN;
            return convert(value.Value);
        }
    }

    internal sealed class AmbientException : Exception
    {
        internal AmbientException(string message) : base(message) { }
    }

    internal sealed class AmbientReading
    {
        internal DateTime TimestampUtc;
        internal double Temperature, DewPoint, Humidity, Pressure, WindSpeed, WindGust, WindDirection, RainRate;
        internal bool IsFresh(DateTime now, int maxAgeSeconds)
        {
            return TimestampUtc <= now.AddMinutes(1) && now - TimestampUtc <= TimeSpan.FromSeconds(maxAgeSeconds);
        }
        internal bool HasSafetyData
        {
            get { return !double.IsNaN(Temperature) && !double.IsNaN(Humidity) &&
                    !double.IsNaN(WindSpeed) && !double.IsNaN(RainRate); }
        }
    }

    [DataContract]
    internal sealed class AmbientDevice
    {
        [DataMember(Name = "macAddress")] public string MacAddress { get; set; }
        [DataMember(Name = "lastData")] public AmbientData LastData { get; set; }
    }

    [DataContract]
    internal sealed class AmbientData
    {
        [DataMember(Name = "dateutc")] public long? DateUtc { get; set; }
        [DataMember(Name = "tempf")] public double? TemperatureF { get; set; }
        [DataMember(Name = "dewPoint")] public double? DewPointF { get; set; }
        [DataMember(Name = "humidity")] public double? Humidity { get; set; }
        [DataMember(Name = "baromabsin")] public double? AbsolutePressure { get; set; }
        [DataMember(Name = "windspeedmph")] public double? WindSpeed { get; set; }
        [DataMember(Name = "windgustmph")] public double? WindGust { get; set; }
        [DataMember(Name = "winddir")] public double? WindDirection { get; set; }
        [DataMember(Name = "hourlyrainin")] public double? HourlyRain { get; set; }
    }
}
