using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace WeatherWatcher2.ObservingConditions
{
    // No hardware access in the parser/state machine, so fault paths can be tested deterministically.
    internal sealed class RainCloudState
    {
        internal string Error = "Waiting for RainCloud data";
        internal bool Dry;
        internal string Rain = "unknown";
        private uint previousSequence, previousUptime;
        private bool seen;
        private long received = -1, clearSince = -1;
        internal long ObservationAt = -1;
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength = 1024, RecursionLimit = 4 };
        internal void Fault(string message) { Error = message; Dry = false; clearSince = -1; }
        private static double Num(Dictionary<string, object> d, string key)
        {
            object v;
            if (!d.TryGetValue(key, out v) || !(v is int || v is long || v is decimal || v is double)) throw new FormatException(key);
            double n = Convert.ToDouble(v, CultureInfo.InvariantCulture);
            if (double.IsNaN(n) || double.IsInfinity(n)) throw new FormatException(key);
            return n;
        }
        private static uint Counter(Dictionary<string, object> d, string key)
        {
            double n = Num(d, key);
            if (n < 0 || n > uint.MaxValue || n != Math.Truncate(n)) throw new FormatException(key);
            return (uint)n;
        }
        private static bool Bool(Dictionary<string, object> d, string key)
        {
            object v;
            if (!d.TryGetValue(key, out v) || !(v is bool)) throw new FormatException(key);
            return (bool)v;
        }
        internal void Accept(string line, long now, int recoverySeconds)
        {
            try
            {
                if (line.Length > 1024) throw new FormatException("oversized record");
                Dictionary<string, object> d;
                lock (Json) d = Json.Deserialize<Dictionary<string, object>>(line);
                if (d == null || Num(d, "v") != 1) throw new FormatException("protocol version");
                uint seq = Counter(d, "seq"), up = Counter(d, "uptime_ms"), age = Counter(d, "sample_age_ms");
                if (seen)
                {
                    uint step = unchecked(seq - previousSequence), elapsed = unchecked(up - previousUptime);
                    if (step == 0 || step > int.MaxValue || elapsed > int.MaxValue)
                    {
                        // Treat reboot/backwards counters as a new baseline but never as a safe sample.
                        previousSequence = seq; previousUptime = up;
                        throw new FormatException("repeated data or board restart");
                    }
                }
                seen = true; previousSequence = seq; previousUptime = up;
                if (received >= 0 && now - received >= 10000) clearSince = -1;
                received = now;
                if (age >= 10000) throw new FormatException("stale rain sample");
                ObservationAt = now - age;
                bool power = Bool(d, "power_ok"), nc = Bool(d, "nc_closed"), no = Bool(d, "no_closed");
                object state;
                if (!d.TryGetValue("rain", out state) || !(state is string)) throw new FormatException("rain state");
                string rain = (string)state;
                if (rain != "dry" && rain != "rain" && rain != "fault" && rain != "hold" && rain != "settling") throw new FormatException("rain state");
                if (!power || nc == no || rain == "fault") throw new FormatException("rain sensor power/contact fault");
                if ((rain == "rain") != no) throw new FormatException("inconsistent rain contacts");
                Rain = rain; // Ignore optional legacy IR fields.
                Dry = rain == "dry" && up >= 30000;
                Error = !Dry ? "Rain or dry recovery pending" : null;
                if (!Dry) clearSince = -1;
                else if (clearSince < 0) clearSince = now;
            }
            catch (Exception ex) { Fault("RainCloud: " + ex.Message); }
        }
        internal bool Fresh(long now) { return received >= 0 && ObservationAt >= 0 && now - received < 10000 && now - ObservationAt < 10000; }
        internal bool Safe(long now, int recoverySeconds)
        {
            if (!Fresh(now)) { clearSince = -1; return false; }
            return Error == null && Dry && clearSince >= 0 && now - clearSince >= recoverySeconds * 1000L;
        }
    }

    internal sealed class RainCloudView
    {
        internal bool Safe, Fresh;
        internal double AgeSeconds;
        internal string Rain;
        internal string Error;
    }
    internal static class RainCloudHub
    {
        private static readonly object Gate = new object();
        private static Session session;
        private static int clients;
        internal static event Action Changed;
        private static void Notify() { var handler = Changed; if (handler != null) handler(); }
        internal static bool InUse { get { lock (Gate) return clients > 0; } }
        internal static void Acquire()
        {
            lock (Gate)
            {
                if (clients == 0) session = new Session(DriverSettings.RainCloudPort);
                clients++;
            }
        }
        internal static void Release()
        {
            lock (Gate)
            {
                if (clients == 0 || --clients != 0) return;
                session.Dispose(); session = null;
            }
        }
        internal static RainCloudView Read()
        {
            lock (Gate) return session == null ? new RainCloudView { Error = "RainCloud is disconnected" } : session.Read();
        }
        private sealed class Session : IDisposable
        {
            private readonly SerialPort port;
            private readonly Thread thread;
            private readonly object gate = new object();
            private readonly Stopwatch clock = Stopwatch.StartNew();
            private readonly RainCloudState state = new RainCloudState();
            private volatile bool stopping;
            internal Session(string name)
            {
                port = new SerialPort(name, 9600, Parity.None, 8, StopBits.One) { ReadTimeout = 250, DtrEnable = true, RtsEnable = false };
                try { port.Open(); } catch { port.Dispose(); throw; }
                thread = new Thread(Run) { IsBackground = true, Name = "RainCloud USB reader" };
                thread.Start();
            }
            private void Run()
            {
                var line = new StringBuilder(); bool discard = false;
                try
                {
                    while (!stopping)
                    {
                        int b;
                        try { b = port.ReadByte(); } catch (TimeoutException) { continue; }
                        if (b == '\n')
                        {
                            lock (gate)
                            {
                                if (discard) state.Fault("Oversized serial record");
                                else if (line.Length > 0) state.Accept(line.ToString(), clock.ElapsedMilliseconds, DriverSettings.RainCloudRecoverySeconds);
                            }
                            Notify();
                            line.Clear(); discard = false;
                        }
                        else if (b != '\r')
                        {
                            if (b < 32 || b > 126 || line.Length >= 1024) { discard = true; lock (gate) state.Fault("Invalid serial record"); }
                            else if (!discard) line.Append((char)b);
                        }
                    }
                }
                catch (Exception ex) { lock (gate) state.Fault("Serial connection failed: " + ex.Message); Notify(); }
            }
            internal RainCloudView Read()
            {
                lock (gate)
                {
                    long now = clock.ElapsedMilliseconds;
                    return new RainCloudView { Safe = state.Safe(now, DriverSettings.RainCloudRecoverySeconds), Fresh = state.Fresh(now), Rain = state.Rain,
                        AgeSeconds = state.ObservationAt < 0 ? double.PositiveInfinity : (now - state.ObservationAt) / 1000.0,
                        Error = state.Fresh(now) ? state.Error : "RainCloud communication timeout" };
                }
            }
            public void Dispose()
            {
                stopping = true;
                // Read timeout bounds the wait; close only after the reader leaves the port.
                thread.Join(1500);
                port.Dispose();
            }
        }
    }
}
