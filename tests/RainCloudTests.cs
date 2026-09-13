using System;
using WeatherWatcher2.ObservingConditions;
internal static class RainCloudTests
{
    private static string Frame(uint seq, uint uptime, string rain = "dry", bool ir = true, bool calibrated = true, int cloud = 10)
    {
        return "{\"v\":1,\"seq\":" + seq + ",\"uptime_ms\":" + uptime + ",\"sample_age_ms\":0,\"rain\":\"" + rain + "\",\"power_ok\":true,\"nc_closed\":" + (rain == "rain" ? "false" : "true") + ",\"no_closed\":" + (rain == "rain" ? "true" : "false") + ",\"ir_ok\":" + ir.ToString().ToLowerInvariant() + ",\"sky_c\":-20,\"sensor_ambient_c\":10,\"delta_c\":-30,\"cloud_calibrated\":" + calibrated.ToString().ToLowerInvariant() + ",\"cloud_estimate_pct\":" + (calibrated ? cloud.ToString() : "null") + "}";
    }
    internal static void Run(Action<bool,string> check)
    {
        var assembly = System.Reflection.Assembly.LoadFrom(System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\SafetyMonitor\bin\Release\ASCOM.WeatherWatcher.SafetyMonitor.dll")));
        var method = assembly.GetType("ASCOM.WeatherWatcher.SafetyMonitor").GetMethod("RainCloudRecordSafe", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var utc = new DateTime(2026,9,13,12,0,0,DateTimeKind.Utc);
        Func<string, bool> safeRecord = text => (bool)method.Invoke(null, new object[] { text, utc });
        check(safeRecord("WW2RC1|" + utc.AddSeconds(-2).Ticks + "|0"), "Fresh safety record");
        check(!safeRecord("WW2RC1|" + utc.AddSeconds(-10).Ticks + "|0"), "Expired safety record");
        check(!safeRecord("WW2RC1|" + utc.AddSeconds(1).Ticks + "|0"), "Future safety timestamp");
        check(!safeRecord("WW2RC1|" + utc.Ticks + "|1"), "Unsafe safety record");
        check(!safeRecord("WW2RC1|bad|0"), "Malformed safety timestamp");
        check(!safeRecord("WW2RC1|0"), "Truncated safety record");        var s = new RainCloudState();
        check(!s.Safe(0,30,5), "RainCloud startup unsafe");
        s.Accept(Frame(1,40000),0,30,5);
        check(!s.Safe(0,30,5) && s.IrOk && s.Cloud == 10, "Recovery and readings");
        s.Accept(Frame(2,46000),6000,30,5);
        check(s.Safe(6000,30,5), "Clear recovery completes");
        check(!s.Safe(16000,30,5), "Ten second expiry");
        s.Accept(Frame(3,57000),17000,30,5);
        check(!s.Safe(17000,30,5), "Stale gap resets recovery");
        s.Accept(Frame(4,63000),23000,30,5);
        check(s.Safe(23000,30,5), "Recovery after gap");
        s.Accept(Frame(4,63000),24000,30,5);
        check(!s.Safe(24000,30,5), "Duplicate rejected");
        s.Accept(Frame(5,65000,"rain"),25000,30,0);
        check(!s.Safe(25000,30,0), "Rain overrides zero recovery");
        s.Accept(Frame(6,67000,calibrated:false),27000,30,0);
        check(!s.Safe(27000,30,0) && double.IsNaN(s.Cloud), "Uncalibrated unsafe");
        s.Accept(Frame(7,69000,ir:false),29000,30,0);
        check(!s.Safe(29000,30,0) && double.IsNaN(s.Sky), "IR failure removes prior values");
        s.Accept(Frame(8,71000,cloud:80),31000,30,0);
        check(!s.Safe(31000,30,0), "Cloud limit");
        s.Accept(Frame(9,73000).Replace("\"power_ok\":true", "\"power_ok\":false"),33000,30,0);
        check(!s.Safe(33000,30,0), "Power failure");
        s.Accept(Frame(10,75000).Replace("\"no_closed\":false", "\"no_closed\":true"),35000,30,0);
        check(!s.Safe(35000,30,0), "Invalid contacts");
        s.Accept(Frame(11,77000).Replace("\"sample_age_ms\":0", "\"sample_age_ms\":10000"),37000,30,0);
        check(!s.Safe(37000,30,0), "Stale sample rejected");
        s.Accept("{}",38000,30,0);
        check(!s.Safe(38000,30,0), "Missing fields");
        s.Accept(Frame(0,1),39000,30,0);
        check(!s.Safe(39000,30,0), "Reboot unsafe");
        s.Accept(Frame(1,2000),40000,30,0);
        check(!s.Safe(40000,30,0), "Board startup guard");
        var wrap = new RainCloudState();
        wrap.Accept(Frame(uint.MaxValue-1,uint.MaxValue-5000),0,30,0);
        wrap.Accept(Frame(uint.MaxValue,uint.MaxValue-3000),2000,30,0);
        wrap.Accept(Frame(0,1000),6000,30,0);
        check(!wrap.Safe(6000,30,0), "Counter wrap remains conservative during uptime guard");
        bool saved = DriverSettings.UseRainCloud, savedAmbient = DriverSettings.UseAmbient, savedCumulus = DriverSettings.UseCumulus, savedBoltwood = DriverSettings.UseBoltwood;
        try
        {
            DriverSettings.UseRainCloud = true; DriverSettings.UseAmbient = DriverSettings.UseCumulus = DriverSettings.UseBoltwood = false;
            var view = new RainCloudView { Safe=true,Fresh=true,IrOk=true,Calibrated=true,Sky=-20,Cloud=10 };
            var reader = new WeatherDataReader(null) { RainCloudRead=()=>view, SafetyWriter=_=>{} };
            reader.Refresh(); check(reader.Data.IsSafe && reader.ReadValue("CloudCover") == 10, "Reader consumes cloud sample");
            reader.Data.RainRate = 5; reader.Refresh(); check(!reader.Data.IsSafe && reader.Data.RainRate == 5, "Existing rain not cleared or fabricated");
            reader.Data.RainRate = 0; view.Safe = false; view.Fresh=false; reader.Refresh();
            check(!reader.Data.IsSafe && double.IsNaN(reader.Data.CloudCover), "Reader invalidates stale sample");
            bool threw=false; try { reader.ReadValue("SkyTemperature"); } catch (ASCOM.DriverException) { threw=true; }
            check(threw,"Stale ASCOM property throws");
        }
        finally { DriverSettings.UseRainCloud=saved;DriverSettings.UseAmbient=savedAmbient;DriverSettings.UseCumulus=savedCumulus;DriverSettings.UseBoltwood=savedBoltwood; }
    }
}
