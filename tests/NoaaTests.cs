using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WeatherWatcher2.ObservingConditions;

internal static class NoaaTests
{
    private static void Wait(NoaaCloudClient client)
    {
        if (!SpinWait.SpinUntil(() => !client.Busy, 5000)) throw new Exception("NOAA background task did not finish");
    }
    internal static void Run(Action<bool, string> check)
    {
        var geo = new GoesProjection { Equatorial=6378137, Polar=6356752.31414, Height=35786023, OriginLongitude=-75 };
        double x,y,lat,lon;
        check(geo.Project(35,-80,out x,out y) && geo.Locate(x,y,out lat,out lon) && Math.Abs(lat-35)<1e-8 && Math.Abs(lon+80)<1e-8, "GOES projection round trip");
        check(geo.Locate(0,0,out lat,out lon) && lat==0 && lon==-75, "GOES sub-satellite pixel");
        check(!geo.Project(35,100,out x,out y), "Far hemisphere rejected");
        check(!geo.Locate(.2,.2,out lat,out lon), "Space pixel rejected");
        check(Math.Abs(GoesProjection.DistanceKm(0,0,0,1)-111.195)<.001, "Geographic sampling distance");
        check(GoesCloudMask.Percentage(40,80,100)==50, "Cloud fraction excludes invalid pixels");
        bool rejected=false; try { GoesCloudMask.Percentage(0,79,100); } catch (InvalidDataException) { rejected=true; }
        check(rejected,"Insufficient quality is unavailable, not clear");
        var now=DateTime.UtcNow;
        var reading=new CloudReading { Percent=100,ObservationUtc=now.AddMinutes(-12),Source="test",ValidPixels=100,TotalPixels=100 };
        check(reading.IsFresh(now,1800) && !reading.IsFresh(now.AddMinutes(18),1800),"NOAA acquisition timestamp controls expiry");
        check(!reading.IsFresh(now.AddMinutes(-13),1800),"Future NOAA timestamp rejected");
        var site=new NoaaSite {Latitude=35,Longitude=-80,RadiusKm=15};
        int calls=0; bool fail=false;
        var client=new NoaaCloudClient((s,previous)=> { Interlocked.Increment(ref calls); if(fail) throw new IOException("offline"); return Task.FromResult(reading); });
        string status;
        client.Get(site,now,600,out status); Wait(client);
        check(client.Get(site,now.AddSeconds(599),600,out status)==reading && calls==1,"NOAA cache and polling cadence");
        fail=true; client.Get(site,now.AddSeconds(600),600,out status); Wait(client);
        check(client.Get(site,now.AddSeconds(601),600,out status)==reading && status.Contains("offline") && calls==2,"Network failure retains previous observation");
        var changed=new NoaaSite {Latitude=36,Longitude=-80,RadiusKm=15};
        check(client.Get(changed,now.AddSeconds(602),600,out status)==null,"Site change invalidates previous site's cloud cache"); Wait(client);
        check(client.Get(new NoaaSite {Latitude=double.NaN},now,600,out status)==null,"Missing coordinates do not produce cloud values");
        var pending=new TaskCompletionSource<CloudReading>();
        var moving=new NoaaCloudClient((s,p)=>s.Latitude==35 ? pending.Task : Task.FromResult(reading));
        moving.Get(site,now,600,out status);
        moving.Get(changed,now,600,out status);
        pending.SetResult(reading); Wait(moving);
        check(moving.Get(changed,now,600,out status)==null,"In-flight previous-site result cannot populate new-site cache"); Wait(moving);

        bool oldRain=DriverSettings.UseRainCloud, oldAmbient=DriverSettings.UseAmbient, oldCumulus=DriverSettings.UseCumulus, oldBoltwood=DriverSettings.UseBoltwood;
        double oldLat=DriverSettings.ObservatoryLatitude, oldLon=DriverSettings.ObservatoryLongitude;
        try
        {
            DriverSettings.UseRainCloud=true; DriverSettings.UseAmbient=DriverSettings.UseCumulus=DriverSettings.UseBoltwood=false;
            DriverSettings.ObservatoryLatitude=35; DriverSettings.ObservatoryLongitude=-80;
            var view=new RainCloudView {Safe=true,Fresh=true,Rain="dry"};
            var ready=new NoaaCloudClient((s,p)=>Task.FromResult(reading));
            ready.Get(site,now,600,out status); Wait(ready);
            var reader=new WeatherDataReader(null) {NoaaClient=ready,RainCloudRead=()=>view,SafetyWriter=_=>{}};
            check(reader.ReadValue("CloudCover")==100 && reader.Data.IsSafe,"Overcast NOAA is informational and SAFE when dry");
            check(reader.TimeSinceLastUpdate("CloudCover")>=720,"ASCOM reports satellite observation age");
            view.Safe=false; view.Rain="rain"; reader.Refresh();
            check(!reader.Data.IsSafe,"Rain is UNSAFE regardless of valid satellite clouds");
            view.Safe=true; view.Rain="dry";
            var blocked=new TaskCompletionSource<CloudReading>();
            reader.NoaaClient=new NoaaCloudClient((s,p)=>blocked.Task);
            var watch=System.Diagnostics.Stopwatch.StartNew(); reader.Refresh();
            check(reader.Data.IsSafe && watch.ElapsedMilliseconds<1000,"Pending NOAA download cannot block rain safety");
            view.Safe=false; reader.Refresh(); check(!reader.Data.IsSafe,"Rain still immediately unsafe during stalled NOAA request");
            blocked.SetException(new IOException("NOAA unavailable")); Wait(reader.NoaaClient);
            view.Safe=true; reader.Refresh(); check(reader.Data.IsSafe,"NOAA outage does not veto dry safety");
            bool threw=false; try { reader.ReadValue("CloudCover"); } catch(ASCOM.DriverException) {threw=true;}
            check(threw,"Unavailable cloud is ASCOM error, not fabricated zero");
            var stale=new NoaaCloudClient((s,p)=>Task.FromResult(new CloudReading {Percent=0,ObservationUtc=now.AddHours(-2)}));
            stale.Get(site,now,600,out status); Wait(stale); reader.NoaaClient=stale; reader.Refresh();
            check(reader.Data.IsSafe && double.IsNaN(reader.Data.CloudCover),"Stale NOAA never affects dry safety");
            threw=false; try { reader.TimeSinceLastUpdate("CloudCover"); } catch(ASCOM.DriverException) {threw=true;}
            check(threw,"Stale cloud age is unavailable through ASCOM");
            reader.Data.WindSpeed=DriverSettings.MaxWind+1; reader.Refresh(); check(!reader.Data.IsSafe,"Existing wind protection retained");
        }
        finally
        {
            DriverSettings.UseRainCloud=oldRain; DriverSettings.UseAmbient=oldAmbient; DriverSettings.UseCumulus=oldCumulus; DriverSettings.UseBoltwood=oldBoltwood;
            DriverSettings.ObservatoryLatitude=oldLat; DriverSettings.ObservatoryLongitude=oldLon;
        }
        // Optional real NOAA fixture test; never downloads or writes a production safety file.
        string fixture=Environment.GetEnvironmentVariable("WEATHERWATCHER_NOAA_FIXTURE");
        if(!string.IsNullOrEmpty(fixture))
        {
            var actual=GoesCloudMask.Read(fixture,site,"fixture");
            check(actual.Percent>=0 && actual.Percent<=100 && actual.ValidPixels>=3,"Decode real NOAA NetCDF/HDF5 cloud mask");
            string exe=Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,@"..\..\WeatherWatcher2.ObservingConditions\bin\Release\ASCOM.WeatherWatcher2.exe"));
            var app=System.Reflection.Assembly.LoadFrom(exe);
            var siteType=app.GetType("WeatherWatcher2.ObservingConditions.NoaaSite");
            var appSite=Activator.CreateInstance(siteType);
            foreach(var item in new[] {new {Name="Latitude",Value=35.0},new {Name="Longitude",Value=-80.0},new {Name="RadiusKm",Value=15.0}})
                siteType.GetField(item.Name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(appSite,item.Value);
            var decode=app.GetType("WeatherWatcher2.ObservingConditions.GoesCloudMask").GetMethod("Read",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            var appResult=decode.Invoke(null,new object[] {fixture,appSite,"fixture"});
            double appPercent=(double)appResult.GetType().GetField("Percent",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(appResult);
            check(appPercent==actual.Percent,"Strong-named WeatherWatcher executable loads PureHDF and decodes NOAA");
            Console.WriteLine("NOAA fixture: "+actual.Percent.ToString("R")+"%; pixels "+actual.ValidPixels+"/"+actual.TotalPixels+"; observation "+actual.ObservationUtc.ToString("O"));
        }
        if(Environment.GetEnvironmentVariable("WEATHERWATCHER_NOAA_LIVE")=="1")
        {
            var live=NoaaCloudClient.Retrieve(site,null).GetAwaiter().GetResult();
            check(live.IsFresh(DateTime.UtcNow,1800),"Live NOAA listing, download, decode and observation age");
            Console.WriteLine("NOAA live: "+live.Percent.ToString("F1")+"%; "+live.ObservationUtc.ToString("O")+"; "+live.Source);
        }
    }
}
