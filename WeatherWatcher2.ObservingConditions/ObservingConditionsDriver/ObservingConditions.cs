using ASCOM.DeviceInterface;
using ASCOM.Utilities;
using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace WeatherWatcher2.ObservingConditions
{
    [Guid("7FCECB35-1C6B-4E48-8F5B-8D4F0E5C1101")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    [ProgId("WeatherWatcher2.ObservingConditions")]
    public class ObservingConditions : IObservingConditionsV2
    {
        private readonly TraceLogger tl;
        private readonly WeatherDataReader reader;
        private bool connected;

        public ObservingConditions()
        {
            tl = new TraceLogger("", "WeatherWatcher2.ObservingConditions");
            tl.Enabled = true;

            reader = new WeatherDataReader(tl);
            connected = false;
        }

        public void SetupDialog()
        {
            using (SetupDialogForm form = new SetupDialogForm())
            {
                form.ShowDialog();
            }
        }

        public ArrayList SupportedActions
        {
            get { return new ArrayList(); }
        }

        public string Action(string actionName, string actionParameters)
        {
            return string.Empty;
        }

        public void CommandBlind(string command, bool raw)
        {
        }

        public bool CommandBool(string command, bool raw)
        {
            return false;
        }

        public string CommandString(string command, bool raw)
        {
            return string.Empty;
        }

        public void Dispose()
        {
            if (tl != null)
            {
                tl.Enabled = false;
                tl.Dispose();
            }
        }

        public bool Connected
        {
            get { return connected; }
            set
            {
                connected = value;

                if (connected)
                {
                    reader.Refresh();
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
            get { return false; }
        }

        public IStateValueCollection DeviceState
        {
            get { return null; }
        }

        public string Description
        {
            get { return "WeatherWatcher2 Observing Conditions Driver"; }
        }

        public string DriverInfo
        {
            get
            {
                return "Reads Cumulus realtime.txt and Boltwood II weather files";
            }
        }

        public string DriverVersion
        {
            get { return "1.0.0"; }
        }

        public short InterfaceVersion
        {
            get { return 2; }
        }

        public string Name
        {
            get { return "WeatherWatcher2.ObservingConditions"; }
        }

        public double AveragePeriod
        {
            get { return 0; }
            set { }
        }

        public double CloudCover
        {
            get
            {
                reader.Refresh();
                return reader.Data.CloudCover;
            }
        }

        public double DewPoint
        {
            get
            {
                reader.Refresh();
                return reader.Data.DewPoint;
            }
        }

        public double Humidity
        {
            get
            {
                reader.Refresh();
                return reader.Data.Humidity;
            }
        }

        public double Pressure
        {
            get
            {
                reader.Refresh();
                return reader.Data.Pressure;
            }
        }

        public double RainRate
        {
            get
            {
                reader.Refresh();
                return reader.Data.RainRate;
            }
        }

        public double SkyBrightness
        {
            get { return 0; }
        }

        public double SkyQuality
        {
            get { return 0; }
        }

        public double SkyTemperature
        {
            get
            {
                reader.Refresh();
                return reader.Data.SkyTemperature;
            }
        }

        public double StarFWHM
        {
            get { return 0; }
        }

        public double Temperature
        {
            get
            {
                reader.Refresh();
                return reader.Data.Temperature;
            }
        }

        public double WindDirection
        {
            get
            {
                reader.Refresh();
                return reader.Data.WindDirection;
            }
        }

        public double WindGust
        {
            get
            {
                reader.Refresh();
                return reader.Data.WindGust;
            }
        }

        public double WindSpeed
        {
            get
            {
                reader.Refresh();
                return reader.Data.WindSpeed;
            }
        }

        public void Refresh()
        {
            reader.Refresh();
        }

        public string SensorDescription(string propertyName)
        {
            return reader.SensorDescription(propertyName);
        }

        public double TimeSinceLastUpdate(string propertyName)
        {
            return reader.TimeSinceLastUpdate(propertyName);
        }

        public double Value(string propertyName)
        {
            return reader.Value(propertyName);
        }
    }
}