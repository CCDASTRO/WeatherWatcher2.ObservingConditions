using System;
using System.Diagnostics;
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
                InitializeAmbientControls();
                LoadSettings();

                tl.LogMessage(
                    "Constructor",
                    "Setup dialog initialized");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    "SETUP DIALOG ERROR",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private CheckBox chkUseAmbient;
        private TextBox txtApplicationKey, txtApiKey, txtMac;
        private NumericUpDown numAmbientAge;
        private LinkLabel updateLink;
        private Label updateWarning;

        private void InitializeAmbientControls()
        {
            ClientSize = new System.Drawing.Size(570, 500);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = true;
            TopMost = true;
            Shown += (sender, args) =>
            {
                BringToFront();
                Activate();
            };
            chkUseAmbient = new CheckBox
            {
                Text = "Use Ambient API instead of Cumulus",
                Location = new System.Drawing.Point(14, 220), AutoSize = true
            };
            Controls.Add(chkUseAmbient);
            txtApplicationKey = AddAmbientText("Application key", 251, true);
            txtApiKey = AddAmbientText("API key", 281, true);
            txtMac = AddAmbientText("Station MAC", 311, false);
            Controls.Add(new Label
            {
                Text = "Maximum age (sec)", Location = new System.Drawing.Point(14, 344), AutoSize = true
            });
            numAmbientAge = new NumericUpDown
            {
                Location = new System.Drawing.Point(145, 341), Width = 100,
                Minimum = 60, Maximum = 3600, Value = 300
            };
            Controls.Add(numAmbientAge);
            Controls.Add(new Label
            {
                Text = "Checks once per minute. Keys are protected for this Windows user.\r\n" +
                       "Boltwood remains independent. Missing or stale Ambient data is unsafe.",
                Location = new System.Drawing.Point(14, 371), Size = new System.Drawing.Size(540, 35)
            });
            updateLink = new LinkLabel
            {
                Text = "Check GitHub regularly for WeatherWatcher updates",
                Location = new System.Drawing.Point(14, 412),
                AutoSize = true
            };
            updateLink.LinkClicked += (sender, args) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://github.com/CCDASTRO/WeatherWatcher2.ObservingConditions/releases",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    tl?.LogMessageCrLf("OpenUpdatesPage", ex.ToString());
                    MessageBox.Show(this,
                        "Could not open the WeatherWatcher GitHub releases page.",
                        "WeatherWatcher2 Updates",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            };
            Controls.Add(updateLink);
            updateWarning = new Label
            {
                Text = "Warning: Close NINA before installing a WeatherWatcher update.",
                Location = new System.Drawing.Point(14, 438),
                AutoSize = true,
                ForeColor = System.Drawing.Color.Firebrick,
                Font = new System.Drawing.Font(Font, System.Drawing.FontStyle.Bold)
            };
            Controls.Add(updateWarning);
            btnOK.Location = new System.Drawing.Point(380, 464);
            btnCancel.Location = new System.Drawing.Point(470, 464);
            chkUseAmbient.CheckedChanged += (sender, args) => UpdateSourceControls();
        }

        private TextBox AddAmbientText(string label, int top, bool secret)
        {
            Controls.Add(new Label
            {
                Text = label, Location = new System.Drawing.Point(14, top + 3), AutoSize = true
            });
            var box = new TextBox
            {
                Location = new System.Drawing.Point(145, top), Width = 400, UseSystemPasswordChar = secret
            };
            Controls.Add(box);
            return box;
        }

        private void UpdateSourceControls()
        {
            bool ambient = chkUseAmbient.Checked;
            chkUseCumulus.Enabled = !ambient;
            txtCumulus.Enabled = !ambient;
            btnBrowseCumulus.Enabled = !ambient;
            txtApplicationKey.Enabled = ambient;
            txtApiKey.Enabled = ambient;
            txtMac.Enabled = ambient;
            numAmbientAge.Enabled = ambient;
        }
        private void LoadSettings()
        {
            chkUseAmbient.Checked = DriverSettings.UseAmbient;
            txtApplicationKey.Text = DriverSettings.AmbientApplicationKey;
            txtApiKey.Text = DriverSettings.AmbientApiKey;
            txtMac.Text = DriverSettings.AmbientMacAddress;
            numAmbientAge.Value = DriverSettings.AmbientMaxAgeSeconds;
            UpdateSourceControls();
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
            try
            {
                using (OpenFileDialog dlg = new OpenFileDialog())
                {
                    dlg.Title = "Select Boltwood File";
                    dlg.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                    dlg.CheckFileExists = true;
                    dlg.Multiselect = false;

                    DialogResult result = dlg.ShowDialog(this);

                    if (result == DialogResult.OK)
                    {
                        txtBoltwood.Text = dlg.FileName;
                        tl?.LogMessage(
                            "BrowseBoltwood",
                            "Selected: " + dlg.FileName);
                    }
                    else
                    {
                        tl?.LogMessage(
                            "BrowseBoltwood",
                            "User cancelled browse");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    "Browse Boltwood Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                tl?.LogMessageCrLf(
                    "BrowseBoltwood",
                    ex.ToString());
            }
        }

        private void btnBrowseCumulus_Click(object sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog dlg = new OpenFileDialog())
                {
                    dlg.Title = "Select Cumulus File";
                    dlg.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                    dlg.CheckFileExists = true;
                    dlg.Multiselect = false;

                    DialogResult result = dlg.ShowDialog(this);

                    if (result == DialogResult.OK)
                    {
                        txtCumulus.Text = dlg.FileName;

                        tl?.LogMessage(
                            "BrowseCumulus",
                            "Selected: " + dlg.FileName);
                    }
                    else
                    {
                        tl?.LogMessage(
                            "BrowseCumulus",
                            "User cancelled browse");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    "Browse Cumulus Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                tl?.LogMessageCrLf(
                    "BrowseCumulus",
                    ex.ToString());
            }
        }
        private void btnOK_Click(object sender, EventArgs e)
        {
            if (chkUseAmbient.Checked &&
                (string.IsNullOrWhiteSpace(txtApplicationKey.Text) || string.IsNullOrWhiteSpace(txtApiKey.Text) ||
                 !System.Text.RegularExpressions.Regex.IsMatch(txtMac.Text.Trim(), @"^([0-9a-fA-F]{2}:){5}[0-9a-fA-F]{2}$")))
            {
                MessageBox.Show(this, "Enter both Ambient keys and a station MAC address (AA:BB:CC:DD:EE:FF).",
                    "Ambient Weather setup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            DriverSettings.UseAmbient = chkUseAmbient.Checked;
            DriverSettings.AmbientApplicationKey = txtApplicationKey.Text.Trim();
            DriverSettings.AmbientApiKey = txtApiKey.Text.Trim();
            DriverSettings.AmbientMacAddress = txtMac.Text.Trim();
            DriverSettings.AmbientMaxAgeSeconds = (int)numAmbientAge.Value;
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

            try { DriverSettings.Save(); }
            catch
            {
                MessageBox.Show(this, "Could not save settings. Check ASCOM profile access.", "WeatherWatcher2",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

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

        private void SetupDialogForm_Load(object sender, EventArgs e)
        {
            LoadSettings();

            lblVersion.Text =
                "Driver Version: " +
                System.Reflection.Assembly
                    .GetExecutingAssembly()
                    .GetName()
                    .Version
                    .ToString(3);
        }
    }
}