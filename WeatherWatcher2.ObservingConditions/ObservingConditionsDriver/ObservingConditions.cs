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
        public static string boltwoodFile = "";
        public static string cumulusFile = "";

        public static double maxWind = 20;
        public static double maxHumidity = 90;
        public static double minTemp = 0;
        public static double maxTemp = 35;

        public static bool useBoltwood = true;
        public static bool useCumulus = true;
        public static bool enableLogging = true;

        private bool connected;
        private readonly TraceLogger tl;

        public ObservingConditions()
        {
            tl = new TraceLogger("", "WeatherWatcher2.ObservingConditions");
            tl.Enabled = true;
            connected = false;
        }

        public void SetupDialog()
        {
            using (SetupDialogForm form = new SetupDialogForm())
            {
                form.ShowDialog();
            }
        }

        public bool Connected
        {
            get { return connected; }
            set { connected = value; }
        }

        public void Connect() { Connected = true; }
        public void Disconnect() { Connected = false; }
        public bool Connecting { get { return false; } }
        public ArrayList SupportedActions { get { return new ArrayList(); } }
        public string Action(string actionName, string actionParameters) { return string.Empty; }
        public void CommandBlind(string command, bool raw) { }
        public bool CommandBool(string command, bool raw) { return false; }
        public string CommandString(string command, bool raw) { return string.Empty; }
        public void Dispose() { if (tl != null) tl.Dispose(); }

        public string Description { get { return "WeatherWatcher2 Observing Conditions Driver"; } }
        public string DriverInfo { get { return "WeatherWatcher2 Driver"; } }
        public string DriverVersion { get { return "1.0.0"; } }
        public short InterfaceVersion { get { return 2; } }
        public string Name { get { return "WeatherWatcher2.ObservingConditions"; } }

        public double AveragePeriod { get { return 0; } set { } }
        public double CloudCover { get { return 0; } }
        public double DewPoint { get { return 0; } }
        public double Humidity { get { return 0; } }
        public double Pressure { get { return 0; } }
        public double RainRate { get { return 0; } }
        public double SkyBrightness { get { return 0; } }
        public double SkyQuality { get { return 0; } }
        public double SkyTemperature { get { return 0; } }
        public double StarFWHM { get { return 0; } }
        public double Temperature { get { return 0; } }
        public double WindDirection { get { return 0; } }
        public double WindGust { get { return 0; } }
        public double WindSpeed { get { return 0; } }

        public void Refresh() { }
        public string SensorDescription(string propertyName) { return propertyName; }
        public double TimeSinceLastUpdate(string propertyName) { return 0; }
        public double Value(string propertyName) { return 0; }
        public IStateValueCollection DeviceState { get { return null; } }
    }
}