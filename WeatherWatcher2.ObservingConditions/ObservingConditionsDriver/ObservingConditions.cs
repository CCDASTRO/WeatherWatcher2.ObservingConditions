using ASCOM;
using ASCOM.DeviceInterface;
using ASCOM.LocalServer;
using ASCOM.Utilities;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace WeatherWatcher2.ObservingConditions
{
    [ServedClassName("WeatherWatcher2 Observing Conditions")]
    [Guid("7FCECB35-1C6B-4E48-8F5B-8D4F0E5C1101")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    [ProgId("WeatherWatcher2.ObservingConditions.ObservingConditions")]
    public class ObservingConditions : IObservingConditionsV2
    {
        private bool connected;
        private System.Threading.Timer ambientTimer;
        private readonly object connectionGate = new object();
        private readonly TraceLogger tl;
        private readonly WeatherDataReader reader;
        public ObservingConditions()
        {
            try
            {
                tl = new TraceLogger(
                    "",
                    "WeatherWatcher2.ObservingConditions");

                // Force ON for debugging
                tl.Enabled = true;

                tl.LogMessage(
                    "Constructor",
                    "Starting constructor");

                ASCOM.LocalServer.LocalServerHost.IncrementObjectCount();

                tl.LogMessage(
                    "Constructor",
                    "IncrementObjectCount OK");

                DriverSettings.Load();

                tl.LogMessage(
                    "Constructor",
                    "DriverSettings.Load OK");

                // Restore user preference after successful load
                tl.Enabled = DriverSettings.EnableLogging;

                reader = new WeatherDataReader(tl);

                tl.LogMessage(
                    "Constructor",
                    "WeatherDataReader created");

                connected = false;

                tl.LogMessage(
                    "Constructor",
                    "ObservingConditions created successfully");
            }
            catch (Exception ex)
            {
                tl?.LogMessageCrLf(
                    "Constructor",
                    ex.ToString());

                try
                {
                    System.Windows.Forms.MessageBox.Show(
                        ex.ToString(),
                        "Constructor Error");
                }
                catch
                {
                }

                throw;
            }
        }

        [ComRegisterFunction]
        public static void RegisterASCOM(Type t)
        {
            using (Profile profile = new Profile())
            {
                profile.DeviceType = "ObservingConditions";

                profile.Register(
                    "WeatherWatcher2.ObservingConditions",
                    "WeatherWatcher2 Observing Conditions");
            }
        }

        [ComUnregisterFunction]
        public static void UnregisterASCOM(Type t)
        {
            using (Profile profile = new Profile())
            {
                profile.DeviceType = "ObservingConditions";

                profile.Unregister(
                    "WeatherWatcher2.ObservingConditions");
            }
        }

        public void SetupDialog()
        {
            Exception threadException = null;

            Thread uiThread = new Thread(() =>
            {
                try
                {
                    using (SetupDialogForm form = new SetupDialogForm())
                    {
                        form.ShowDialog();
                    }
                }
                catch (Exception ex)
                {
                    threadException = ex;
                }
            });

            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.Start();
            uiThread.Join();

            if (threadException != null)
            {
                MessageBox.Show(
                    threadException.ToString(),
                    "SetupDialog Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        #region Connection

        public bool Connected
        {
            get
            {
                return connected;
            }
            set
            {
                lock (connectionGate)
                {
                    if (connected == value) return;
                    connected = value;
                    if (value)
                    {
                        ambientTimer = new System.Threading.Timer(_ =>
                        {
                            lock (connectionGate)
                            {
                                if (!connected || !DriverSettings.UseAmbient) return;
                                try { reader.Refresh(); }
                                catch { tl?.LogMessage("Ambient", "Background refresh failed."); }
                            }
                        }, null, 0, 5000);
                    }
                    else
                    {
                        ambientTimer?.Dispose();
                        ambientTimer = null;
                    }
                }

                if (tl != null)
                {
                    tl.LogMessage(
                        "Connected",
                        value ? "Connected" : "Disconnected");
                }
            }
        }

        public void Connect()
        {
            Connected = true;
        }

        public void Disconnect()
        {
            Connected = false;
        }

        public bool Connecting
        {
            get
            {
                return false;
            }
        }

        #endregion

        #region Common ASCOM Members

        public ArrayList SupportedActions
        {
            get
            {
                return new ArrayList();
            }
        }

        public string Action(
            string actionName,
            string actionParameters)
        {
            return string.Empty;
        }

        public void CommandBlind(
            string command,
            bool raw)
        {
        }

        public bool CommandBool(
            string command,
            bool raw)
        {
            return false;
        }

        public string CommandString(
            string command,
            bool raw)
        {
            return string.Empty;
        }

        public void Dispose()
        {
            try
            {
                tl?.LogMessage(
                    "Dispose",
                    "Driver disposing");

                Connected = false;
                LocalServerHost.DecrementObjectCount();
                LocalServerHost.ExitIf();
            }
            catch (Exception ex)
            {
                tl?.LogMessageCrLf(
                    "Dispose",
                    ex.ToString());
            }
            finally
            {
                tl?.Dispose();
            }
        }
        public string Description
        {
            get
            {
                return "WeatherWatcher2 Observing Conditions Driver";
            }
        }

        public string DriverInfo
        {
            get
            {
                return "Reads Boltwood and Cumulus weather files, or AmbientWeather.net instead of Cumulus.";
            }
        }

        public string DriverVersion
        {
            get
            {
                return System.Reflection.Assembly
                 .GetExecutingAssembly()
                 .GetName()
                 .Version
                 .ToString(3);
            }
        }

        public short InterfaceVersion
        {
            get
            {
                return 2;
            }
        }

        public string Name
        {
            get
            {
                return "WeatherWatcher2 Observing Conditions";
            }
        }

        #endregion

        #region Weather Properties

        private double averagePeriod = 0;

        public double AveragePeriod
        {
            get
            {
                return averagePeriod;
            }
            set
            {
                if (value < 0)
                    throw new ASCOM.InvalidValueException(
                        "AveragePeriod",
                        value.ToString(),
                        "Value must be >= 0");

                averagePeriod = value;
            }
        }

        public double CloudCover
        {
            get
            {
                return reader.ReadValue("CloudCover");
            }
        }

        public double DewPoint
        {
            get
            {
                return reader.ReadValue("DewPoint");
            }
        }

        public double Humidity
        {
            get
            {
                return reader.ReadValue("Humidity");
            }
        }

        public double Pressure
        {
            get
            {
                return reader.ReadValue("Pressure");
            }
        }

        public double RainRate
        {
            get
            {
                return reader.ReadValue("RainRate");
            }
        }

        public double SkyBrightness
        {
            get
            {
                throw new PropertyNotImplementedException(
                    "SkyBrightness",
                    false);
            }
        }

        public double SkyQuality
        {
            get
            {
                throw new PropertyNotImplementedException(
                    "SkyQuality",
                    false);
            }
        }

        public double SkyTemperature
        {
            get
            {
                return reader.ReadValue("SkyTemperature");
            }
        }

        public double StarFWHM
        {
            get
            {
                throw new PropertyNotImplementedException(
                    "StarFWHM",
                    false);
            }
        }

        public double Temperature
        {
            get
            {
                return reader.ReadValue("Temperature");
            }
        }

        public double WindDirection
        {
            get
            {
                return reader.ReadValue("WindDirection");
            }
        }

        public double WindGust
        {
            get
            {
                return reader.ReadValue("WindGust");
            }
        }

        public double WindSpeed
        {
            get
            {
                return reader.ReadValue("WindSpeed");
            }
        }

        #endregion

        #region Required Methods

        public void Refresh()
        {
            reader.Refresh();

            tl?.LogMessage(
                "Refresh",
                "Weather data refreshed");
        }
        public string SensorDescription(string propertyName)
        {
            switch (propertyName)
            {
                case "SkyBrightness":
                case "SkyQuality":
                case "StarFWHM":
                    throw new ASCOM.PropertyNotImplementedException(propertyName, false);

                default:
                    return propertyName;
            }
        }

        public double TimeSinceLastUpdate(string propertyName)
        {
            switch (propertyName)
            {
                case "SkyBrightness":
                case "SkyQuality":
                case "StarFWHM":
                    throw new ASCOM.PropertyNotImplementedException(propertyName, false);

                default:
                    return reader.TimeSinceLastUpdate(propertyName);
            }
        }

        public double Value(
            string propertyName)
        {
            return 0;
        }

        public IStateValueCollection DeviceState
        {
            get
            {
                return new StateValueCollection();
            }
        }

        #endregion
    }
}