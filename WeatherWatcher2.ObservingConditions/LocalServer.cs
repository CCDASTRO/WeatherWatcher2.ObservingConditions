using ASCOM.Utilities;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ASCOM.LocalServer
{
    public static class LocalServerHost
    {
        private static uint mainThreadId;
        private static bool startedByCOM;
        private static FrmMain localServerMainForm;

        private static int driversInUseCount;
        private static int serverLockCount;

        private static ArrayList driverTypes;
        private static ArrayList classFactories;

        private static readonly object lockObject = new object();

        private static TraceLogger TL;
        private static Task gcTask;
        private static CancellationTokenSource gcTokenSource;

        private const string DriverId =
            "WeatherWatcher2.ObservingConditions";

        private const string DriverDescription =
            "WeatherWatcher2 Observing Conditions";

        private const string DriverCLSID =
            "{7FCECB35-1C6B-4E48-8F5B-8D4F0E5C1101}";

        [STAThread]
        static void Main(string[] args)
        {
            TL = new TraceLogger("", "WeatherWatcher2.LocalServer");
            TL.Enabled = true;

            try
            {
                TL.LogMessage("Main", "Local Server starting");

                if (!PopulateListOfAscomDrivers())
                    return;

                if (!ProcessArguments(args))
                    return;

                driversInUseCount = 0;
                serverLockCount = 0;
                mainThreadId = GetCurrentThreadId();

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                localServerMainForm = new FrmMain();

                if (startedByCOM)
                    localServerMainForm.WindowState =
                        FormWindowState.Minimized;

                if (!RegisterClassFactories())
                    return;

                StartGarbageCollection(10000);

                Application.Run(localServerMainForm);
            }
            catch (Exception ex)
            {
                TL.LogMessageCrLf("Main", ex.ToString());

                MessageBox.Show(
                    ex.ToString(),
                    "Local Server Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                RevokeClassFactories();
                StopGarbageCollection();

                TL.LogMessage("Main", "Local Server closing");
                TL.Dispose();
            }
        }

        public static int ObjectCount
        {
            get { return driversInUseCount; }
        }

        public static int ServerLockCount
        {
            get { return serverLockCount; }
        }

        public static int IncrementObjectCount()
        {
            return Interlocked.Increment(ref driversInUseCount);
        }

        public static int DecrementObjectCount()
        {
            return Interlocked.Decrement(ref driversInUseCount);
        }

        public static int IncrementServerLockCount()
        {
            return Interlocked.Increment(ref serverLockCount);
        }

        public static int DecrementServerLockCount()
        {
            return Interlocked.Decrement(ref serverLockCount);
        }

        public static void ExitIf()
        {
            lock (lockObject)
            {
                if (ObjectCount <= 0 &&
                    ServerLockCount <= 0 &&
                    startedByCOM)
                {
                    PostThreadMessage(
                        mainThreadId,
                        0x0012,
                        UIntPtr.Zero,
                        IntPtr.Zero);
                }
            }
        }

        private static bool PopulateListOfAscomDrivers()
        {
            driverTypes = new ArrayList();

            try
            {
                Type[] types =
                    Assembly.GetExecutingAssembly().GetTypes();

                foreach (Type type in types)
                {
                    object[] attributes =
                        type.GetCustomAttributes(
                            typeof(ServedClassNameAttribute),
                            false);

                    if (attributes.Length > 0)
                    {
                        driverTypes.Add(type);

                        TL.LogMessage(
                            "PopulateListOfAscomDrivers",
                            "Driver found: " + type.FullName);
                    }
                }

                TL.LogMessage(
                    "PopulateListOfAscomDrivers",
                    "Total drivers found: " +
                    driverTypes.Count.ToString());

                return driverTypes.Count > 0;
            }
            catch (Exception ex)
            {
                TL.LogMessageCrLf(
                    "PopulateListOfAscomDrivers",
                    ex.ToString());

                return false;
            }
        }

        private static bool ProcessArguments(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                TL.LogMessage(
                    "ProcessArguments",
                    "No arguments supplied - normal/manual launch");

                startedByCOM = false;
                return true;
            }

            string allArgs = string.Join(" | ", args);

            TL.LogMessage(
                "ProcessArguments",
                "Received arguments: " + allArgs);

            string arg = args[0].ToLowerInvariant();

            switch (arg)
            {
                case "-embedding":
                case "/embedding":
                case "-embedded":
                case "/embedded":

                    TL.LogMessage(
                        "ProcessArguments",
                        "COM launch detected");

                    startedByCOM = true;
                    return true;

                case "-register":
                case "/register":
                case "-regserver":
                case "/regserver":

                    RegisterObjects();
                    return false;

                case "-unregister":
                case "/unregister":
                case "-unregserver":
                case "/unregserver":

                    UnregisterObjects();
                    return false;

                default:
                    TL.LogMessage(
                        "ProcessArguments",
                        "Unknown startup argument: " + arg);

                    MessageBox.Show(
                        "Unknown startup argument: " + arg,
                        "ASCOM Local Server",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return false;
            }
        }

        private static void RegisterObjects()
        {
            try
            {
                TL.LogMessage(
                    "RegisterObjects",
                    "Starting registration");

                if (!IsAdministrator)
                {
                    TL.LogMessage(
                        "RegisterObjects",
                        "Elevation required");

                    ElevateSelf("-register");
                    return;
                }

                string exePath =
                    Assembly.GetExecutingAssembly().Location;

                //
                // ASCOM Chooser registration
                //
                using (Profile profile = new Profile())
                {
                    profile.DeviceType = "ObservingConditions";
                    profile.Register(
                        DriverId,
                        DriverDescription);
                }

                //
                // ProgID registration
                //
                Registry.SetValue(
                    @"HKEY_CLASSES_ROOT\" + DriverId,
                    "",
                    DriverDescription);

                Registry.SetValue(
                    @"HKEY_CLASSES_ROOT\" +
                    DriverId +
                    @"\CLSID",
                    "",
                    DriverCLSID);

                //
                // CLSID registration
                //
                Registry.SetValue(
                    @"HKEY_CLASSES_ROOT\CLSID\" +
                    DriverCLSID,
                    "",
                    DriverDescription);

                Registry.SetValue(
                    @"HKEY_CLASSES_ROOT\CLSID\" +
                    DriverCLSID +
                    @"\ProgID",
                    "",
                    DriverId);

                //
                // CRITICAL FIX:
                // MUST use full EXE path with quotes
                //
                Registry.SetValue(
                    @"HKEY_CLASSES_ROOT\CLSID\" +
                    DriverCLSID +
                    @"\LocalServer32",
                    "",
                    "\"" + exePath + "\"");

                //
                // CRITICAL FIX:
                // AppID registration required for COM activation
                //
                Registry.SetValue(
                    @"HKEY_CLASSES_ROOT\CLSID\" +
                    DriverCLSID +
                    @"\AppID",
                    "",
                    DriverCLSID);

                Registry.SetValue(
                    @"HKEY_CLASSES_ROOT\AppID\" +
                    DriverCLSID,
                    "",
                    DriverDescription);

                Registry.SetValue(
                    @"HKEY_CLASSES_ROOT\AppID\" +
                    DriverCLSID,
                    "RunAs",
                    "Interactive User");

                TL.LogMessage(
                    "RegisterObjects",
                    "Registration complete");

                
            }
            catch (Exception ex)
            {
                TL.LogMessageCrLf(
                    "RegisterObjects",
                    ex.ToString());

                
            }
        }

        private static void UnregisterObjects()
        {
            try
            {
                TL.LogMessage(
                    "UnregisterObjects",
                    "Starting unregistration");

                if (!IsAdministrator)
                {
                    TL.LogMessage(
                        "UnregisterObjects",
                        "Elevation required");

                    ElevateSelf("-unregister");
                    return;
                }

                using (Profile profile = new Profile())
                {
                    profile.DeviceType = "ObservingConditions";
                    profile.Unregister(DriverId);
                }

                try
                {
                    Registry.ClassesRoot.DeleteSubKeyTree(
                        DriverId,
                        false);
                }
                catch { }

                try
                {
                    Registry.ClassesRoot.DeleteSubKeyTree(
                        @"CLSID\" + DriverCLSID,
                        false);
                }
                catch { }

                try
                {
                    Registry.ClassesRoot.DeleteSubKeyTree(
                        @"AppID\" + DriverCLSID,
                        false);
                }
                catch { }

                TL.LogMessage(
                    "UnregisterObjects",
                    "Unregistration complete");

                
            }
            catch (Exception ex)
            {
                TL.LogMessageCrLf(
                    "UnregisterObjects",
                    ex.ToString());

                
            }
        }

        private static bool RegisterClassFactories()
        {
            classFactories = new ArrayList();

            try
            {
                foreach (Type type in driverTypes)
                {
                    ClassFactory factory =
                        new ClassFactory(type);

                    classFactories.Add(factory);

                    if (!factory.RegisterClassObject())
                    {
                        TL.LogMessage(
                            "RegisterClassFactories",
                            "Failed for: " + type.Name);

                        

                        return false;
                    }
                }

                ClassFactory.ResumeClassObjects();

                TL.LogMessage(
                    "RegisterClassFactories",
                    "Class factories registered");

                return true;
            }
            catch (Exception ex)
            {
                TL.LogMessageCrLf(
                    "RegisterClassFactories",
                    ex.ToString());

                return false;
            }
        }

        private static void RevokeClassFactories()
        {
            try
            {
                ClassFactory.SuspendClassObjects();

                if (classFactories == null)
                    return;

                foreach (ClassFactory factory in classFactories)
                {
                    if (factory != null)
                        factory.RevokeClassObject();
                }
            }
            catch (Exception ex)
            {
                TL.LogMessageCrLf(
                    "RevokeClassFactories",
                    ex.ToString());
            }
        }

        private static void StartGarbageCollection(
            int interval)
        {
            try
            {
                gcTokenSource =
                    new CancellationTokenSource();

                GarbageCollection gc =
                    new GarbageCollection(interval);

                gcTask = Task.Factory.StartNew(
                    () => gc.GCWatch(
                        gcTokenSource.Token),
                    gcTokenSource.Token,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);
            }
            catch (Exception ex)
            {
                TL.LogMessageCrLf(
                    "StartGarbageCollection",
                    ex.ToString());
            }
        }

        private static void StopGarbageCollection()
        {
            try
            {
                if (gcTokenSource == null)
                    return;

                gcTokenSource.Cancel();

                if (gcTask != null &&
                    !gcTask.IsCompleted)
                {
                    gcTask.Wait(3000);
                }

                gcTask = null;

                gcTokenSource.Dispose();
                gcTokenSource = null;
            }
            catch (Exception ex)
            {
                TL.LogMessageCrLf(
                    "StopGarbageCollection",
                    ex.ToString());
            }
        }

        private static bool IsAdministrator
        {
            get
            {
                using (WindowsIdentity identity =
                    WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal =
                        new WindowsPrincipal(identity);

                    return principal.IsInRole(
                        WindowsBuiltInRole.Administrator);
                }
            }
        }

        private static void ElevateSelf(string argument)
        {
            try
            {
                ProcessStartInfo psi =
                    new ProcessStartInfo();

                psi.FileName =
                    Application.ExecutablePath;

                psi.Arguments = argument;
                psi.Verb = "runas";
                psi.UseShellExecute = true;

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                TL?.LogMessageCrLf(
        "ElevateSelf",
        ex.ToString());

                MessageBox.Show(
                    ex.Message,
                    "Elevation Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        [DllImport("user32.dll")]
        private static extern bool PostThreadMessage(
            uint idThread,
            uint Msg,
            UIntPtr wParam,
            IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();
    }
}