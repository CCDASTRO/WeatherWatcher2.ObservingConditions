using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using WeatherWatcher2.ObservingConditions;

internal static class AmbientTests
{
    private const string Mac = "AA:BB:CC:DD:EE:FF";
    private static int checks;
    private static void Check(bool passed, string message)
    {
        if (!passed) throw new Exception(message);
        checks++;
    }
    private static void Near(double actual, double expected, string message)
    {
        Check(Math.Abs(actual - expected) < 0.0001, message);
    }
    private static void Throws<T>(Action action, string message) where T : Exception
    {
        try { action(); }
        catch (T) { checks++; return; }
        throw new Exception(message);
    }
    private static string Json(DateTime time, string fields = null)
    {
        fields = fields ?? "\"tempf\":68,\"dewPoint\":50,\"humidity\":55,\"baromabsin\":29.92," +
            "\"windspeedmph\":10,\"windgustmph\":15,\"winddir\":180,\"hourlyrainin\":0";
        return "[{\"macAddress\":\"" + Mac + "\",\"lastData\":{\"dateutc\":" +
            new DateTimeOffset(time).ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture) + "," + fields + "}}]";
    }
    private static AmbientReading Wait(AmbientWeatherClient client)
    {
        AmbientReading result = null;
        string error;
        Check(SpinWait.SpinUntil(() =>
        {
            result = client.Get("app", "api", Mac, out error);
            return result != null && error == null;
        }, 3000), "Background request did not finish");
        return result;
    }
    private static void CheckSetupDialog()
    {
        System.Windows.Forms.Application.EnableVisualStyles();
        string exe = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            @"..\..\WeatherWatcher2.ObservingConditions\bin\Release\ASCOM.WeatherWatcher2.exe"));
        var assembly = Assembly.LoadFrom(exe);
        var settings = assembly.GetType("WeatherWatcher2.ObservingConditions.DriverSettings");
        settings.GetField("UseAmbient").SetValue(null, true);
        var type = assembly.GetType("WeatherWatcher2.ObservingConditions.SetupDialogForm");
        using (var form = (System.Windows.Forms.Form)Activator.CreateInstance(type))
        {
            Func<string, object> field = name => type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(form);
            var ambient = (System.Windows.Forms.CheckBox)field("chkUseAmbient");
            var cumulus = (System.Windows.Forms.CheckBox)field("chkUseCumulus");
            var boltwood = (System.Windows.Forms.CheckBox)field("chkUseBoltwood");
            Check(ambient.Checked && !cumulus.Enabled && boltwood.Enabled, "Ambient UI source selection");
            Check(((System.Windows.Forms.TextBox)field("txtApiKey")).UseSystemPasswordChar, "API key mask");
            Check(form.TopMost, "Setup dialog must remain above the calling application");
            Check(form.ShowInTaskbar, "Setup dialog must remain discoverable on the taskbar");
            Check(form.StartPosition == System.Windows.Forms.FormStartPosition.CenterScreen, "Setup dialog start position");
            var updateLink = (System.Windows.Forms.LinkLabel)field("updateLink");
            Check(updateLink.Text.Contains("GitHub") && updateLink.Text.Contains("updates"), "Update reminder link");
            var updateWarning = (System.Windows.Forms.Label)field("updateWarning");
            Check(updateWarning.Text.Contains("Close NINA") && updateWarning.ForeColor == System.Drawing.Color.Firebrick,
                "Update installation warning");
            var okButton = (System.Windows.Forms.Button)field("btnOK");
            var cancelButton = (System.Windows.Forms.Button)field("btnCancel");
            Check(!updateWarning.Bounds.IntersectsWith(okButton.Bounds) &&
                  !updateWarning.Bounds.IntersectsWith(cancelButton.Bounds),
                "Update warning must not overlap dialog buttons");
            form.ShowInTaskbar = false;
            form.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            form.Location = new System.Drawing.Point(-32000, -32000);
            form.Show();
            System.Windows.Forms.Application.DoEvents();
            using (var bitmap = new System.Drawing.Bitmap(form.Width, form.Height))
            {
                form.DrawToBitmap(bitmap, new System.Drawing.Rectangle(0, 0, form.Width, form.Height));
                bitmap.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ambient-setup.png"));
            }
            ambient.Checked = false;
            Check(cumulus.Enabled && boltwood.Enabled, "File controls restored");
            foreach (System.Windows.Forms.Control control in form.Controls)
                Check(control.Right <= form.ClientSize.Width && control.Bottom <= form.ClientSize.Height,
                    "Control outside dialog: " + control.Text);
        }
    }
    [STAThread]
    private static void Main()
    {
        DateTime now = DateTime.UtcNow;
        var data = AmbientWeatherClient.Parse(Json(now), Mac.ToLowerInvariant(), now);
        Near(data.Temperature, 20, "Fahrenheit conversion");
        Near(data.DewPoint, 10, "Dew point conversion");
        Near(data.Pressure, 1013.207489, "Absolute pressure conversion");
        Near(data.WindSpeed, 4.4704, "Wind conversion");
        Near(data.WindGust, 6.7056, "Gust conversion");
        Near(data.WindDirection, 180, "Wind direction");
        Near(AmbientWeatherClient.Parse(Json(now, "\"hourlyrainin\":0.1"), Mac, now).RainRate, 2.54, "Rain conversion");
        Throws<System.Runtime.Serialization.SerializationException>(() => AmbientWeatherClient.Parse("{bad", Mac, now),
            "Malformed API JSON accepted");
        Check(data.HasSafetyData && data.IsFresh(now, 300), "Complete fresh observation");
        Check(!data.IsFresh(now.AddSeconds(301), 300), "Stale observation");
        var sparse = AmbientWeatherClient.Parse(Json(now, "\"tempf\":32"), Mac, now);
        Near(sparse.Temperature, 0, "Zero Celsius is valid");
        Check(double.IsNaN(sparse.WindSpeed) && !sparse.HasSafetyData, "Missing fields must not become zero");
        var invalid = AmbientWeatherClient.Parse(Json(now, "\"humidity\":101,\"windspeedmph\":-1"), Mac, now);
        Check(double.IsNaN(invalid.Humidity) && double.IsNaN(invalid.WindSpeed), "Invalid sensor range");
        Throws<AmbientException>(() => AmbientWeatherClient.Parse(Json(now), "wrong", now), "Wrong station accepted");
        Throws<AmbientException>(() => AmbientWeatherClient.Parse("[]", Mac, now), "Empty devices accepted");
        Throws<AmbientException>(() => AmbientWeatherClient.Parse(Json(now.AddMinutes(5)), Mac, now), "Future date accepted");

        DateTime clock = now;
        int calls = 0;
        var completion = new TaskCompletionSource<string>();
        var client = new AmbientWeatherClient
        {
            UtcNow = () => clock,
            Download = (app, api) => { Interlocked.Increment(ref calls); return completion.Task; }
        };
        string failure;
        Check(client.Get("app", "api", Mac, out failure) == null, "First request should not block");
        Check(SpinWait.SpinUntil(() => calls == 1, 3000), "Request not started");
        for (int i = 0; i < 100; i++) client.Get("app", "api", Mac, out failure);
        Check(calls == 1, "Concurrent property reads must share one request");
        completion.SetResult(Json(now));
        Wait(client);
        clock = now.AddSeconds(59);
        client.Get("app", "api", Mac, out failure);
        Check(calls == 1, "Cooldown not honored");
        client.Download = (app, api) => { throw new Exception("secret-api-key-in-url"); };
        clock = now.AddSeconds(61);
        client.Get("app", "api", Mac, out failure);
        string problem = null;
        Check(SpinWait.SpinUntil(() => { client.Get("app", "api", Mac, out problem); return problem != null; }, 3000),
            "Failure did not surface");
        Check(!problem.Contains("secret"), "Credential leaked through error");
        var retained = client.Get("app", "api", Mac, out failure);
        Check(retained.TimestampUtc == data.TimestampUtc, "Failed request refreshed observation age");
        Check(client.Get("app", "other", Mac, out failure) == null, "Changed credentials reused old station data");
        clock = now.AddSeconds(122);
        client.Download = (app, api) => Task.FromResult(Json(now));
        Check(SpinWait.SpinUntil(() => client.Get("app", "other", Mac, out failure) != null, 3000), "Recovery failed");

        string cumulus = Path.GetTempFileName();
        string boltwood = Path.GetTempFileName();
        try
        {
            File.WriteAllText(cumulus, "28/08/26 12:00:00 99 55 10 2 3 180 0 0 1000");
            File.WriteAllText(boltwood, "date time C K -25 15 0 0 0 0 0 0 0");
            DriverSettings.CumulusFile = cumulus;
            DriverSettings.BoltwoodFile = boltwood;
            DriverSettings.UseCumulus = true;
            DriverSettings.UseBoltwood = true;
            DriverSettings.UseAmbient = false;
            var reader = new WeatherDataReader(null) { SafetyWriter = safe => { } };
            reader.Refresh();
            Near(reader.Data.Temperature, 99, "Cumulus file source regressed");
            Near(reader.Data.SkyTemperature, -25, "Boltwood file source regressed");

            DriverSettings.UseAmbient = true;
            DriverSettings.AmbientApplicationKey = "app";
            DriverSettings.AmbientApiKey = "api";
            DriverSettings.AmbientMacAddress = Mac;
            var source = new AmbientWeatherClient { Download = (app, api) => Task.FromResult(Json(now)) };
            reader.AmbientClient = source;
            reader.Refresh();
            Wait(source);
            reader.Refresh();
            Near(reader.Data.Temperature, 20, "Ambient must replace Cumulus");
            Near(reader.Data.SkyTemperature, -25, "Boltwood must remain available with Ambient");
            Check(reader.Data.IsSafe, "Fresh dry low-wind data should pass existing thresholds");
            Check(reader.TimeSinceLastUpdate("Temperature") >= 0, "Observation age");
            File.WriteAllText(boltwood, "date time C K -25 15 0 0 0 0 0 1 0");
            reader.Refresh();
            Check(!reader.Data.IsSafe, "Boltwood rain must mark unsafe");
            Near(reader.Data.RainRate, 0, "Boltwood flag must not overwrite Ambient numerical rain rate");

            File.SetLastWriteTimeUtc(boltwood, now.AddMinutes(-10));
            reader.Refresh();
            Check(!reader.Data.IsSafe, "Stale enabled Boltwood must be unsafe");
            Throws<ASCOM.DriverException>(() => reader.ReadValue("SkyTemperature"), "Stale Boltwood value returned");

            DriverSettings.UseBoltwood = false;
            reader.Refresh();
            Check(reader.Data.IsSafe, "Disabled Boltwood still contributed rain");
            Throws<ASCOM.PropertyNotImplementedException>(() => reader.ReadValue("SkyTemperature"), "Absent sky sensor reported");

            var failed = new AmbientWeatherClient
            {
                Download = (app, api) => { throw new Exception("test network outage"); }
            };
            reader.AmbientClient = failed;
            reader.Refresh();
            Check(!reader.Data.IsSafe, "Unavailable API must be unsafe");
            Throws<ASCOM.DriverException>(() => reader.ReadValue("Temperature"), "Unavailable API returned a temperature");

            var stale = new AmbientWeatherClient { Download = (app, api) => Task.FromResult(Json(now.AddMinutes(-10))) };
            reader.AmbientClient = stale;
            reader.Refresh();
            Wait(stale);
            reader.Refresh();
            Check(!reader.Data.IsSafe, "Stale Ambient must be unsafe");
            Throws<ASCOM.DriverException>(() => reader.ReadValue("Temperature"), "Stale property returned a value");
            Check(reader.TimeSinceLastUpdate("Temperature") >= 600, "Stale timestamp hidden");

            var missing = new AmbientWeatherClient { Download = (app, api) => Task.FromResult(Json(now, "\"tempf\":68")) };
            reader.AmbientClient = missing;
            reader.Refresh();
            Wait(missing);
            reader.Refresh();
            Check(!reader.Data.IsSafe, "Missing safety sensors must be unsafe");
            Throws<ASCOM.PropertyNotImplementedException>(() => reader.ReadValue("RainRate"), "Missing rain returned zero");

            DriverSettings.UseAmbient = false;
            reader.Refresh();
            Near(reader.Data.Temperature, 99, "Switching back to Cumulus");
        }
        finally
        {
            File.Delete(cumulus);
            File.Delete(boltwood);
        }
        var protect = typeof(DriverSettings).GetMethod("Protect", BindingFlags.Static | BindingFlags.NonPublic);
        var unprotect = typeof(DriverSettings).GetMethod("Unprotect", BindingFlags.Static | BindingFlags.NonPublic);
        string encrypted = (string)protect.Invoke(null, new object[] { "test-only-key" });
        Check(encrypted != "test-only-key", "Credential stored as plaintext");
        Check((string)unprotect.Invoke(null, new object[] { encrypted }) == "test-only-key", "Credential roundtrip");
        Check((string)unprotect.Invoke(null, new object[] { "not base64!" }) == "", "Corrupt credential handling");
        CheckSetupDialog();
        RainCloudTests.Run(Check);
        Console.WriteLine("PASS: " + checks + " checks (no live API or production safety-file writes).");
    }
}
