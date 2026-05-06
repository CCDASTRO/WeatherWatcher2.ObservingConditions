using System;
using System.Windows.Forms;

namespace WeatherWatcher2.ObservingConditions
{
    public partial class SetupDialogForm : Form
    {
        public SetupDialogForm()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            txtBoltwood.Text = ProfileManager.Read("BoltwoodPath", "");
            txtCumulus.Text = ProfileManager.Read("CumulusPath", "");
            txtMaxWind.Text = ProfileManager.Read("MaxWind", "20");
            txtMaxHumidity.Text = ProfileManager.Read("MaxHumidity", "90");
            txtMinTemp.Text = ProfileManager.Read("MinTemp", "0");
        }

        private void SaveSettings()
        {
            ProfileManager.Write("BoltwoodPath", txtBoltwood.Text);
            ProfileManager.Write("CumulusPath", txtCumulus.Text);
            ProfileManager.Write("MaxWind", txtMaxWind.Text);
            ProfileManager.Write("MaxHumidity", txtMaxHumidity.Text);
            ProfileManager.Write("MinTemp", txtMinTemp.Text);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            SaveSettings();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}