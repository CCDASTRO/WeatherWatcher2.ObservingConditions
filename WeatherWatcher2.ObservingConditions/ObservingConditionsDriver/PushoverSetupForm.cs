using System;
using System.Drawing;
using System.Windows.Forms;

namespace WeatherWatcher2.ObservingConditions
{
    internal sealed class PushoverSetupForm : Form
    {
        private readonly CheckBox enabled, clearing, availability;
        private readonly TextBox token, user, device;
        private readonly NumericUpDown warning, clear, cooldown;
        private readonly Button test;
        private readonly Label status;
        internal PushoverSettings Settings { get; private set; }
        internal PushoverSetupForm(PushoverSettings settings)
        {
            Settings = settings.Copy();
            Text = "WeatherWatcher — Pushover cloud notifications";
            ClientSize = new Size(580, 475);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = MinimizeBox = false;
            enabled = new CheckBox { Text = "Enable cloud notifications while WeatherWatcher is connected", Left = 16, Top = 16, AutoSize = true, Checked = settings.Enabled };
            Controls.Add(enabled);
            token = AddText("Application API token", 50, settings.Token, true);
            user = AddText("User or group key", 84, settings.User, true);
            device = AddText("Device (optional)", 118, settings.Device, false);
            warning = AddNumber("Warn at / above (%)", 159, 1, 100, settings.WarnPercent);
            clear = AddNumber("Clearing at / below (%)", 193, 0, 99, settings.ClearPercent);
            cooldown = AddNumber("Cooldown (minutes)", 227, 1, 1440, settings.CooldownMinutes);
            clearing = new CheckBox { Text = "Notify when clouds clear after a warning", Left = 16, Top = 265, AutoSize = true, Checked = settings.NotifyClearing };
            availability = new CheckBox { Text = "Notify when NOAA data becomes unavailable or recovers", Left = 16, Top = 293, AutoSize = true, Checked = settings.NotifyAvailability };
            Controls.Add(clearing); Controls.Add(availability);
            Controls.Add(new Label { Text = "Credentials are protected for this Windows user. Normal priority respects quiet hours.\r\nNOAA cloud notifications never change SAFE/UNSAFE or control the dome.", Left = 16, Top = 324, Width = 548, Height = 40 });
            test = new Button { Text = "Send test notification", Left = 16, Top = 375, Width = 180, Height = 28 };
            test.Click += SendTest;
            Controls.Add(test);
            status = new Label { Left = 208, Top = 375, Width = 352, Height = 42 };
            Controls.Add(status);
            var ok = new Button { Text = "OK", Left = 390, Top = 430, Width = 80 };
            ok.Click += (sender, args) => {
                var candidate = ReadSettings();
                string error = candidate.Enabled ? candidate.ValidationError() : null;
                if (error != null) { MessageBox.Show(this, error, "Pushover settings"); return; }
                Settings = candidate; DialogResult = DialogResult.OK;
            };
            var cancel = new Button { Text = "Cancel", Left = 480, Top = 430, Width = 80, DialogResult = DialogResult.Cancel };
            Controls.Add(ok); Controls.Add(cancel); AcceptButton = ok; CancelButton = cancel;
        }
        private TextBox AddText(string label, int top, string value, bool secret)
        {
            Controls.Add(new Label { Text = label, Left = 16, Top = top + 3, AutoSize = true });
            var box = new TextBox { Text = value, Left = 190, Top = top, Width = 370, UseSystemPasswordChar = secret };
            Controls.Add(box); return box;
        }
        private NumericUpDown AddNumber(string label, int top, int minimum, int maximum, int value)
        {
            Controls.Add(new Label { Text = label, Left = 16, Top = top + 3, AutoSize = true });
            var number = new NumericUpDown { Left = 190, Top = top, Width = 100, Minimum = minimum, Maximum = maximum, Value = Math.Max(minimum, Math.Min(maximum, value)) };
            Controls.Add(number); return number;
        }
        private PushoverSettings ReadSettings()
        {
            return new PushoverSettings { Enabled = enabled.Checked, Token = token.Text.Trim(), User = user.Text.Trim(), Device = device.Text.Trim(),
                WarnPercent = (int)warning.Value, ClearPercent = (int)clear.Value, CooldownMinutes = (int)cooldown.Value,
                NotifyClearing = clearing.Checked, NotifyAvailability = availability.Checked };
        }
        private async void SendTest(object sender, EventArgs args)
        {
            var candidate = ReadSettings();
            string error = candidate.ValidationError();
            if (error != null) { status.Text = error; return; }
            test.Enabled = false; status.Text = "Sending test...";
            try
            {
                await PushoverClient.Send(candidate, new CloudNotice { Title = "WeatherWatcher — Test notification",
                    Message = "Pushover cloud notifications are working. This is a test, not a weather observation. Safety status is unchanged." });
                if (!IsDisposed) status.Text = "Pushover accepted the test. Check your device.";
            }
            catch { if (!IsDisposed) status.Text = "Not sent. Check credentials, Internet access and Pushover quota."; }
            finally { if (!IsDisposed) test.Enabled = true; }
        }
    }
}
