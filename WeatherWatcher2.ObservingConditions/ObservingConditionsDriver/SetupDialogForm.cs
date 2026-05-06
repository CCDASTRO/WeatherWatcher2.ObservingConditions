using System;
using System.Windows.Forms;
using ASCOM.Utilities;

namespace WeatherWatcher2.ObservingConditions
{
    public partial class SetupDialogForm : Form
    {
        private readonly TraceLogger tl;

        public SetupDialogForm()
        {
            try
            {
                tl = new TraceLogger(
                    "",
                    "WeatherWatcher2.SetupDialog");

                tl.Enabled = true;

                InitializeComponent();
                LoadSettings();
            }
            catch (Exception ex)
            {
                tl?.LogMessageCrLf("Constructor", ex.ToString());
            }
        }

        private void LoadSettings()
        {
            txtBoltwood.Text = DriverSettings.BoltwoodFile;
            txtCumulus.Text = DriverSettings.CumulusFile;

            txtMaxWind.Text = DriverSettings.MaxWind.ToString();
            txtMaxHumidity.Text = DriverSettings.MaxHumidity.ToString();
            txtMinTemp.Text = DriverSettings.MinTemp.ToString();
            txtMaxTemp.Text = DriverSettings.MaxTemp.ToString();

            chkUseBoltwood.Checked = DriverSettings.UseBoltwood;
            chkUseCumulus.Checked = DriverSettings.UseCumulus;
            chkEnableLogging.Checked = DriverSettings.EnableLogging;

            tl.LogMessage("LoadSettings", "Settings loaded into dialog");
        }

        private void btnBrowseBoltwood_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "Select Boltwood File";
                dlg.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtBoltwood.Text = dlg.FileName;
                    tl.LogMessage("BrowseBoltwood", dlg.FileName);
                }
            }
        }

        private void btnBrowseCumulus_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "Select Cumulus File";
                dlg.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtCumulus.Text = dlg.FileName;
                    tl.LogMessage("BrowseCumulus", dlg.FileName);
                }
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            DriverSettings.BoltwoodFile = txtBoltwood.Text;
            DriverSettings.CumulusFile = txtCumulus.Text;

            if (double.TryParse(txtMaxWind.Text, out double maxWind))
                DriverSettings.MaxWind = maxWind;

            if (double.TryParse(txtMaxHumidity.Text, out double maxHumidity))
                DriverSettings.MaxHumidity = maxHumidity;

            if (double.TryParse(txtMinTemp.Text, out double minTemp))
                DriverSettings.MinTemp = minTemp;

            if (double.TryParse(txtMaxTemp.Text, out double maxTemp))
                DriverSettings.MaxTemp = maxTemp;

            DriverSettings.UseBoltwood = chkUseBoltwood.Checked;
            DriverSettings.UseCumulus = chkUseCumulus.Checked;
            DriverSettings.EnableLogging = chkEnableLogging.Checked;

            DriverSettings.Save();

            tl.LogMessage("btnOK_Click", "Settings saved");

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            tl.LogMessage("btnCancel_Click", "User cancelled setup");

            DialogResult = DialogResult.Cancel;
            Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                tl?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}