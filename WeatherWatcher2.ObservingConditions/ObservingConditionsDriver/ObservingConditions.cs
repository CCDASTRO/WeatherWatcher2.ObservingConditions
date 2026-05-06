using ASCOM;
using ASCOM.DeviceInterface;
using ASCOM.LocalServer;
using ASCOM.Utilities;
using System;
using System.Collections;
using System.Runtime.InteropServices;
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
        private readonly TraceLogger tl;

        public ObservingConditions()
        {
            try
            {
                //MessageBox.Show("Constructor Fired", "DEBUG");
                ASCOM.LocalServer.LocalServerHost.IncrementObjectCount();
                DriverSettings.Load();

                tl = new TraceLogger(
                    "",
                    "WeatherWatcher2.ObservingConditions");

                tl.Enabled = DriverSettings.EnableLogging;
                connected = false;

                tl.LogMessage(
                    "Constructor",
                    "ObservingConditions created successfully");
            }
            catch (Exception ex)
            {
                tl?.LogMessageCrLf("Constructor",ex.ToString());

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
                    "WeatherWatcher2.ObservingConditions.ObservingConditions",
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
                    "WeatherWatcher2.ObservingConditions.ObservingConditions");
            }
        }

        public void SetupDialog()
        {
            try
            {
                //MessageBox.Show("SetupDialog started");

                using (SetupDialogForm form = new SetupDialogForm())
                {
                    //MessageBox.Show("Form created");

                    form.ShowDialog();

                    //MessageBox.Show("Form closed");
                }
            }
            catch (Exception ex)
            {
                tl?.LogMessageCrLf("Constructor", ex.ToString());
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
                connected = value;

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
                return "Reads weather conditions from Boltwood and/or Cumulus weather files.";
            }
        }

        public string DriverVersion
        {
            get
            {
                return "1.0.0";
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

        public double AveragePeriod
        {
            get
            {
                return 0;
            }
            set
            {
            }
        }

        public double CloudCover
        {
            get
            {
                return 0;
            }
        }

        public double DewPoint
        {
            get
            {
                return 0;
            }
        }

        public double Humidity
        {
            get
            {
                return 0;
            }
        }

        public double Pressure
        {
            get
            {
                return 0;
            }
        }

        public double RainRate
        {
            get
            {
                throw new PropertyNotImplementedException(
                    "RainRate",
                    false);
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
                return 0;
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
                return 0;
            }
        }

        public double WindDirection
        {
            get
            {
                return 0;
            }
        }

        public double WindGust
        {
            get
            {
                return 0;
            }
        }

        public double WindSpeed
        {
            get
            {
                return 0;
            }
        }

        #endregion

        #region Required Methods

        public void Refresh()
        {
            if (tl != null)
            {
                tl.LogMessage(
                    "Refresh",
                    "Refresh called");
            }
        }

        public string SensorDescription(
            string propertyName)
        {
            return propertyName;
        }

        public double TimeSinceLastUpdate(
            string propertyName)
        {
            return 0;
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
                return null;
            }
        }

        #endregion
    }
}