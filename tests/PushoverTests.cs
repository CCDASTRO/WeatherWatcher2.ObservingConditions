using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using WeatherWatcher2.ObservingConditions;

internal static class PushoverTests
{
    private static CloudReading Reading(DateTime time, double percent) { return new CloudReading { ObservationUtc=time, Percent=percent }; }
    private static void Wait(CloudNotifier notifier) { if(!SpinWait.SpinUntil(()=>!notifier.Busy,3000)) throw new Exception("Notification worker stalled"); }
    private sealed class Handler : HttpMessageHandler
    {
        internal string Body, Address, Verb;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
        {
            Address=request.RequestUri.ToString(); Verb=request.Method.Method; Body=await request.Content.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK) { Content=new StringContent("{\"status\":1,\"request\":\"mock\"}") };
        }
    }
    internal static void Run(Action<bool,string> check)
    {
        var settings=new PushoverSettings {Enabled=true,Token=new string('a',30),User=new string('b',30)};
        check(settings.ValidationError()==null,"Pushover valid credentials/settings");
        var invalid=settings.Copy(); invalid.ClearPercent=31; check(invalid.ValidationError()!=null,"Reject inverted cloud thresholds");
        invalid=settings.Copy(); invalid.Token=""; check(invalid.ValidationError()!=null,"Reject absent Pushover token");
        invalid=settings.Copy(); invalid.Device="invalid device"; check(invalid.ValidationError()!=null,"Reject invalid target device");
        var now=new DateTime(2026,9,18,18,0,0,DateTimeKind.Utc);
        var state=new CloudAlertState();
        check(state.Evaluate(Reading(now,10),now,1800,15,settings)==null,"No initial clear-weather notification");
        var warning=state.Evaluate(Reading(now.AddMinutes(1),30),now.AddMinutes(1),1800,15,settings);
        check(warning!=null && warning.Value && warning.Message.Contains("safety status unchanged") && warning.Message.Contains("15 km"),"Warning at threshold includes advisory context");
        state.Delivered(warning);
        check(state.Evaluate(Reading(now.AddMinutes(2),29),now.AddMinutes(2),1800,15,settings)==null,"Hysteresis suppresses threshold chatter");
        var clear=state.Evaluate(Reading(now.AddMinutes(3),20),now.AddMinutes(3),1800,15,settings);
        check(clear!=null && !clear.Value,"Clearing at lower threshold"); state.Delivered(clear);
        check(state.Evaluate(Reading(now.AddMinutes(3),80),now.AddMinutes(3),1800,15,settings)==null,"Same observation cannot create a second transition");
        check(state.Evaluate(Reading(now.AddHours(-1),80),now.AddMinutes(4),1800,15,settings)==null,"Stale data cannot generate cloud warning");
        check(state.Evaluate(Reading(now.AddHours(1),80),now.AddMinutes(4),1800,15,settings)==null,"Future data cannot generate cloud warning");
        var availability=new CloudAlertState(); var availabilitySettings=settings.Copy(); availabilitySettings.NotifyAvailability=true;
        check(availability.Evaluate(null,now,1800,15,availabilitySettings)==null,"Initial retrieval grace prevents immediate missing-data alert");
        var lost=availability.Evaluate(null,now.AddMinutes(30),1800,15,availabilitySettings);
        check(lost!=null && lost.Availability && lost.Value,"Optional unavailable notice after grace"); availability.Delivered(lost);
        var restored=availability.Evaluate(Reading(now.AddMinutes(31),0),now.AddMinutes(31),1800,15,availabilitySettings);
        check(restored!=null && restored.Availability && !restored.Value,"Optional data-restored notice");

        var site=new NoaaSite {Latitude=35,Longitude=-80,RadiusKm=15}; int sends=0;
        var notifier=new CloudNotifier((s,n)=>{Interlocked.Increment(ref sends);return Task.CompletedTask;});
        var cloudy=Reading(now,80);
        notifier.Process(cloudy,now,1800,site,settings); Wait(notifier);
        notifier.Process(cloudy,now,1800,site,settings); Wait(notifier);
        check(sends==1,"Shared notifier deduplicates multiple clients and repeated observations");
        notifier.Process(Reading(now.AddMinutes(1),0),now.AddMinutes(1),1800,site,settings);
        check(sends==1,"Cooldown holds clearing notice");
        notifier.Process(Reading(now.AddMinutes(30),0),now.AddMinutes(30),1800,site,settings); Wait(notifier);
        check(sends==2,"Current pending clearing notice sent after cooldown");
        var disabled=settings.Copy(); disabled.Enabled=false;
        notifier.Process(Reading(now.AddMinutes(31),80),now.AddMinutes(31),1800,site,disabled); Wait(notifier);
        check(sends==2,"Disabled notifications send nothing");
        int failures=0;
        var failed=new CloudNotifier((s,n)=>{Interlocked.Increment(ref failures);throw new IOException("mock offline");});
        failed.Process(cloudy,now,1800,site,settings); Wait(failed);
        failed.Process(cloudy,now.AddMinutes(1),1800,site,settings); Wait(failed);
        check(failures==1,"Delivery failure does not create a retry storm");
        failed.Process(Reading(now.AddMinutes(30),80),now.AddMinutes(30),1800,site,settings); Wait(failed);
        check(failures==2,"Failed alert retries after cooldown against fresh current conditions");

        using(var handler=new Handler()) using(var http=new HttpClient(handler))
        {
            PushoverClient.SendWith(http,settings,warning).GetAwaiter().GetResult();
            check(handler.Address=="https://api.pushover.net/1/messages.json" && handler.Verb=="POST","Pushover uses official HTTPS POST endpoint");
            check(handler.Body.Contains("priority=0") && handler.Body.Contains("ttl=1800") && handler.Body.Contains("token="),"Normal priority, expiry and encoded credentials");
        }
        bool rejected=false;try{PushoverClient.ValidateResponse(true,"{\"status\":0}");}catch(InvalidOperationException){rejected=true;}
        check(rejected,"HTTP success with API rejection is a failed notification");
        rejected=false;try{PushoverClient.ValidateResponse(false,"{\"status\":1}");}catch(InvalidOperationException){rejected=true;}
        check(rejected,"HTTP failure cannot be reported delivered");

        var previous=DriverSettings.Pushover; bool oldRain=DriverSettings.UseRainCloud, oldAmbient=DriverSettings.UseAmbient, oldCumulus=DriverSettings.UseCumulus, oldBoltwood=DriverSettings.UseBoltwood;
        double oldLat=DriverSettings.ObservatoryLatitude,oldLon=DriverSettings.ObservatoryLongitude;
        var pending=new TaskCompletionSource<bool>();
        var slow=new CloudNotifier((s,n)=>pending.Task);
        try
        {
            DriverSettings.Pushover=settings; DriverSettings.UseRainCloud=true; DriverSettings.UseAmbient=DriverSettings.UseCumulus=DriverSettings.UseBoltwood=false;
            DriverSettings.ObservatoryLatitude=35;DriverSettings.ObservatoryLongitude=-80;
            DateTime actual=DateTime.UtcNow;
            var noaa=new NoaaCloudClient((s,p)=>Task.FromResult(Reading(actual,80)));
            string status;noaa.Get(site,actual,600,out status);
            check(SpinWait.SpinUntil(()=>!noaa.Busy,3000),"Mock NOAA data ready");
            var rain=new RainCloudView {Safe=true,Fresh=true,Rain="dry"}; bool published=false;
            var reader=new WeatherDataReader(null) {NoaaClient=noaa,Notifier=slow,NotificationsActive=true,RainCloudRead=()=>rain,SafetyWriter=s=>published=s};
            var watch=System.Diagnostics.Stopwatch.StartNew();reader.Refresh();
            check(watch.ElapsedMilliseconds<1000 && published && slow.Busy,"Blocked Pushover does not delay SAFE publication");
            rain.Safe=false;reader.Refresh();check(!published && !reader.Data.IsSafe,"Rain immediately UNSAFE during blocked notification");
            pending.SetException(new IOException("mock delivery failure"));Wait(slow);
            rain.Safe=true;reader.Refresh();check(published,"Pushover failure never vetoes dry safety");
            int disconnectedSends=0;
            reader.Notifier=new CloudNotifier((s,n)=>{Interlocked.Increment(ref disconnectedSends);return Task.CompletedTask;});reader.NotificationsActive=false;
            reader.Refresh();check(disconnectedSends==0,"Disconnected reader cannot initiate notifications");
        }
        finally
        {
            pending.TrySetResult(true);DriverSettings.Pushover=previous; DriverSettings.UseRainCloud=oldRain;DriverSettings.UseAmbient=oldAmbient;DriverSettings.UseCumulus=oldCumulus;DriverSettings.UseBoltwood=oldBoltwood;
            DriverSettings.ObservatoryLatitude=oldLat;DriverSettings.ObservatoryLongitude=oldLon;
        }
        CheckDialog(check);
    }
    private static void CheckDialog(Action<bool,string> check)
    {
        var app=Assembly.LoadFrom(Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,@"..\..\WeatherWatcher2.ObservingConditions\bin\Release\ASCOM.WeatherWatcher2.exe")));
        var type=app.GetType("WeatherWatcher2.ObservingConditions.PushoverSetupForm");
        var settings=Activator.CreateInstance(app.GetType("WeatherWatcher2.ObservingConditions.PushoverSettings"));
        using(var form=(System.Windows.Forms.Form)Activator.CreateInstance(type,BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{settings},null))
        {
            foreach(string name in new[]{"token","user"}) check(((System.Windows.Forms.TextBox)type.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(form)).UseSystemPasswordChar,"Pushover "+name+" masked");
            form.ShowInTaskbar=false;form.StartPosition=System.Windows.Forms.FormStartPosition.Manual;form.Location=new System.Drawing.Point(-32000,-32000);form.Show();System.Windows.Forms.Application.DoEvents();
            foreach(System.Windows.Forms.Control control in form.Controls) check(control.Right<=form.ClientSize.Width && control.Bottom<=form.ClientSize.Height,"Pushover control within dialog: "+control.Text);
            using(var bitmap=new System.Drawing.Bitmap(form.Width,form.Height)) {form.DrawToBitmap(bitmap,new System.Drawing.Rectangle(0,0,form.Width,form.Height));bitmap.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"pushover-setup.png"));}
        }
    }
}
