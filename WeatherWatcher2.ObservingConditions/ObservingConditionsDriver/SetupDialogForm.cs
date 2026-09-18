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
                InitializeRainCloudControls();
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

        private PushoverSettings pushoverSettings;
        private CheckBox chkRainCloud;
        private TextBox txtRainPort;
        private NumericUpDown numDryDelay, numNoaaPoll, numNoaaAge, numNoaaRadius;
        private TextBox txtLatitude, txtLongitude;
        private void InitializeRainCloudControls()
        {
            ClientSize = new System.Drawing.Size(570, 805);
            chkRainCloud = new CheckBox { Text = "Use Arduino RG-11 rain protection and NOAA cloud information", AutoSize = true, Left = 14, Top = 465 };
            txtRainPort = new TextBox { Left = 145, Top = 492, Width = 95 };
            numDryDelay = new NumericUpDown { Left = 430, Top = 492, Width = 95, Minimum = 0, Maximum = 3600 };
            chkRainCloud.CheckedChanged += (sender, args) => { chkUseBoltwood.Enabled = !chkRainCloud.Checked; txtBoltwood.Enabled = !chkRainCloud.Checked; btnBrowseBoltwood.Enabled = !chkRainCloud.Checked; };
            Controls.Add(chkRainCloud); Controls.Add(txtRainPort); Controls.Add(numDryDelay);
            Controls.Add(new Label { Text = "Uno COM port", Left = 14, Top = 496, AutoSize = true });
            Controls.Add(new Label { Text = "RG-11 dry-out (sec)", Left = 265, Top = 496, AutoSize = true });
            Controls.Add(new Label { Text = "Wet, fault or lost Arduino connection = UNSAFE. Clouds are informational.\r\nRequires WeatherWatcher SafetyMonitor with WW2RC1 support.", Left = 14, Top = 528, Width = 540, Height = 44 });
            Controls.Add(new Label { Text = "NOAA GOES-East cloud mask (latitude north / longitude east positive)", Left = 14, Top = 576, AutoSize = true });
            txtLatitude = AddAmbientText("Latitude (degrees)", 600, false);
            txtLongitude = AddAmbientText("Longitude (degrees)", 630, false);
            numNoaaRadius = AddNoaaNumber("Sampling radius (km)", 660, 5, 100);
            numNoaaPoll = AddNoaaNumber("NOAA poll (sec)", 690, 600, 3600);
            numNoaaAge = AddNoaaNumber("Stale after (sec)", 720, 600, 86400);
            var pushButton = new Button { Text = "Cloud notifications...", Left = 320, Top = 686, Width = 225, Height = 30 };
            pushButton.Click += (sender, args) => {
                using (var dialog = new PushoverSetupForm(pushoverSettings))
                    if (dialog.ShowDialog(this) == DialogResult.OK) pushoverSettings = dialog.Settings;
            };
            Controls.Add(pushButton);
            Controls.Add(new Label { Text = "Optional Pushover advisories.\r\nNever changes safety status.", Left = 320, Top = 724, Width = 230, Height = 32 });
            btnOK.Top = btnCancel.Top = 766;
        }
        private NumericUpDown AddNoaaNumber(string label, int top, int minimum, int maximum)
        {
            Controls.Add(new Label { Text = label, Left = 14, Top = top + 3, AutoSize = true });
            var number = new NumericUpDown { Left = 180, Top = top, Width = 100, Minimum = minimum, Maximum = maximum };
            Controls.Add(number);
            return number;
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
            pushoverSettings = DriverSettings.Pushover.Copy();
            chkRainCloud.Checked = DriverSettings.UseRainCloud;
            txtRainPort.Text = DriverSettings.RainCloudPort;
            numDryDelay.Value = DriverSettings.RainCloudRecoverySeconds;
            txtLatitude.Text = double.IsNaN(DriverSettings.ObservatoryLatitude) ? "" : DriverSettings.ObservatoryLatitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            txtLongitude.Text = double.IsNaN(DriverSettings.ObservatoryLongitude) ? "" : DriverSettings.ObservatoryLongitude.ToString(System.Globalization.CultureInfo.InvariantCulture);
            numNoaaRadius.Value = (decimal)DriverSettings.NoaaRadiusKm;
            numNoaaPoll.Value = DriverSettings.NoaaPollSeconds;
            numNoaaAge.Value = DriverSettings.NoaaStaleSeconds;
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
            if (RainCloudHub.InUse) { MessageBox.Show(this, "Disconnect all WeatherWatcher clients before changing settings."); return; }
            if (chkRainCloud.Checked && !System.Text.RegularExpressions.Regex.IsMatch(txtRainPort.Text.Trim(), @"^COM[1-9][0-9]*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) { MessageBox.Show(this, "Enter the Uno COM port, for example COM5."); return; }
            if (chkUseAmbient.Checked &&
                (string.IsNullOrWhiteSpace(txtApplicationKey.Text) || string.IsNullOrWhiteSpace(txtApiKey.Text) ||
                 !System.Text.RegularExpressions.Regex.IsMatch(txtMac.Text.Trim(), @"^([0-9a-fA-F]{2}:){5}[0-9a-fA-F]{2}$")))
            {
                MessageBox.Show(this, "Enter both Ambient keys and a station MAC address (AA:BB:CC:DD:EE:FF).",
                    "Ambient Weather setup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            double latitude = double.NaN, longitude = double.NaN;
            if (txtLatitude.Text.Trim().Length != 0 || txtLongitude.Text.Trim().Length != 0)
            {
                if (!double.TryParse(txtLatitude.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out latitude) ||
                    !double.TryParse(txtLongitude.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out longitude) ||
                    !(latitude >= -90 && latitude <= 90 && longitude >= -180 && longitude <= 180))
                { MessageBox.Show(this, "Enter latitude -90 to 90 and longitude -180 to 180 using a decimal point, or leave both blank for rain-only operation."); return; }
            }
            if (numNoaaAge.Value < numNoaaPoll.Value) { MessageBox.Show(this, "NOAA stale timeout must be at least the polling interval."); return; }
            DriverSettings.ObservatoryLatitude = latitude;
            DriverSettings.ObservatoryLongitude = longitude;
            DriverSettings.NoaaRadiusKm = (double)numNoaaRadius.Value;
            DriverSettings.NoaaPollSeconds = (int)numNoaaPoll.Value;
            DriverSettings.NoaaStaleSeconds = (int)numNoaaAge.Value;
            DriverSettings.UseRainCloud = chkRainCloud.Checked;
            DriverSettings.RainCloudPort = txtRainPort.Text.Trim().ToUpperInvariant();
            DriverSettings.RainCloudRecoverySeconds = (int)numDryDelay.Value;
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

            DriverSettings.Pushover = pushoverSettings.Copy();
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
