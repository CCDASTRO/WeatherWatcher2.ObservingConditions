using System;
using System.Windows.Forms;
using ASCOM.Utilities;

namespace WeatherWatcher2.ObservingConditions
{
    public partial class SetupDialogForm : Form
    {
        private TraceLogger tl;

        public SetupDialogForm()
        {
            InitializeComponent();

            tl = new TraceLogger("", "WeatherWatcher2.Setup");
            tl.Enabled = true;

            LoadSettings();
        }

        private void LoadSettings()
        {
            txtBoltwood.Text = ObservingConditions.boltwoodFile;
            txtCumulus.Text = ObservingConditions.cumulusFile;
            txtMaxWind.Text = ObservingConditions.maxWind.ToString();
            txtMaxHumidity.Text = ObservingConditions.maxHumidity.ToString();
            txtMinTemp.Text = ObservingConditions.minTemp.ToString();
            txtMaxTemp.Text = ObservingConditions.maxTemp.ToString();

            chkUseBoltwood.Checked = ObservingConditions.useBoltwood;
            chkUseCumulus.Checked = ObservingConditions.useCumulus;
            chkEnableLogging.Checked = ObservingConditions.enableLogging;
        }

        private void btnBrowseBoltwood_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtBoltwood.Text = dlg.FileName;
            }
        }

        private void btnBrowseCumulus_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtCumulus.Text = dlg.FileName;
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            ObservingConditions.boltwoodFile = txtBoltwood.Text;
            ObservingConditions.cumulusFile = txtCumulus.Text;

            double.TryParse(txtMaxWind.Text, out ObservingConditions.maxWind);
            double.TryParse(txtMaxHumidity.Text, out ObservingConditions.maxHumidity);
            double.TryParse(txtMinTemp.Text, out ObservingConditions.minTemp);
            double.TryParse(txtMaxTemp.Text, out ObservingConditions.maxTemp);

            ObservingConditions.useBoltwood = chkUseBoltwood.Checked;
            ObservingConditions.useCumulus = chkUseCumulus.Checked;
            ObservingConditions.enableLogging = chkEnableLogging.Checked;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}