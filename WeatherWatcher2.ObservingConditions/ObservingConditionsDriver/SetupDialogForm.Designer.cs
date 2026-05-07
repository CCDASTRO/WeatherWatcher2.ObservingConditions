namespace WeatherWatcher2.ObservingConditions
{
    partial class SetupDialogForm
    {
        private System.Windows.Forms.TextBox txtBoltwood;
        private System.Windows.Forms.TextBox txtCumulus;
        private System.Windows.Forms.TextBox txtMaxWind;
        private System.Windows.Forms.TextBox txtMaxHumidity;
        private System.Windows.Forms.TextBox txtMinTemp;
        private System.Windows.Forms.TextBox txtMaxTemp;

        private System.Windows.Forms.CheckBox chkUseBoltwood;
        private System.Windows.Forms.CheckBox chkUseCumulus;
        private System.Windows.Forms.CheckBox chkEnableLogging;

        private System.Windows.Forms.Button btnBrowseBoltwood;
        private System.Windows.Forms.Button btnBrowseCumulus;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SetupDialogForm));
            this.txtBoltwood = new System.Windows.Forms.TextBox();
            this.txtCumulus = new System.Windows.Forms.TextBox();
            this.txtMaxWind = new System.Windows.Forms.TextBox();
            this.txtMaxHumidity = new System.Windows.Forms.TextBox();
            this.txtMinTemp = new System.Windows.Forms.TextBox();
            this.txtMaxTemp = new System.Windows.Forms.TextBox();
            this.chkUseBoltwood = new System.Windows.Forms.CheckBox();
            this.chkUseCumulus = new System.Windows.Forms.CheckBox();
            this.chkEnableLogging = new System.Windows.Forms.CheckBox();
            this.btnBrowseBoltwood = new System.Windows.Forms.Button();
            this.btnBrowseCumulus = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.lblVersion = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            this.SuspendLayout();
            // 
            // txtBoltwood
            // 
            this.txtBoltwood.Location = new System.Drawing.Point(132, 13);
            this.txtBoltwood.Name = "txtBoltwood";
            this.txtBoltwood.Size = new System.Drawing.Size(250, 20);
            this.txtBoltwood.TabIndex = 0;
            // 
            // txtCumulus
            // 
            this.txtCumulus.Location = new System.Drawing.Point(132, 39);
            this.txtCumulus.Name = "txtCumulus";
            this.txtCumulus.Size = new System.Drawing.Size(250, 20);
            this.txtCumulus.TabIndex = 1;
            // 
            // txtMaxWind
            // 
            this.txtMaxWind.Location = new System.Drawing.Point(132, 93);
            this.txtMaxWind.Name = "txtMaxWind";
            this.txtMaxWind.Size = new System.Drawing.Size(100, 20);
            this.txtMaxWind.TabIndex = 2;
            // 
            // txtMaxHumidity
            // 
            this.txtMaxHumidity.Location = new System.Drawing.Point(132, 123);
            this.txtMaxHumidity.Name = "txtMaxHumidity";
            this.txtMaxHumidity.Size = new System.Drawing.Size(100, 20);
            this.txtMaxHumidity.TabIndex = 3;
            // 
            // txtMinTemp
            // 
            this.txtMinTemp.Location = new System.Drawing.Point(132, 153);
            this.txtMinTemp.Name = "txtMinTemp";
            this.txtMinTemp.Size = new System.Drawing.Size(100, 20);
            this.txtMinTemp.TabIndex = 4;
            // 
            // txtMaxTemp
            // 
            this.txtMaxTemp.Location = new System.Drawing.Point(132, 183);
            this.txtMaxTemp.Name = "txtMaxTemp";
            this.txtMaxTemp.Size = new System.Drawing.Size(100, 20);
            this.txtMaxTemp.TabIndex = 5;
            // 
            // chkUseBoltwood
            // 
            this.chkUseBoltwood.Location = new System.Drawing.Point(14, 13);
            this.chkUseBoltwood.Name = "chkUseBoltwood";
            this.chkUseBoltwood.Size = new System.Drawing.Size(112, 20);
            this.chkUseBoltwood.TabIndex = 6;
            this.chkUseBoltwood.Text = "Enable Boltwood";
            // 
            // chkUseCumulus
            // 
            this.chkUseCumulus.Location = new System.Drawing.Point(14, 39);
            this.chkUseCumulus.Name = "chkUseCumulus";
            this.chkUseCumulus.Size = new System.Drawing.Size(112, 20);
            this.chkUseCumulus.TabIndex = 7;
            this.chkUseCumulus.Text = "Enable Cumulus";
            // 
            // chkEnableLogging
            // 
            this.chkEnableLogging.Location = new System.Drawing.Point(14, 65);
            this.chkEnableLogging.Name = "chkEnableLogging";
            this.chkEnableLogging.Size = new System.Drawing.Size(114, 20);
            this.chkEnableLogging.TabIndex = 8;
            this.chkEnableLogging.Text = "Enable Logging";
            // 
            // btnBrowseBoltwood
            // 
            this.btnBrowseBoltwood.Location = new System.Drawing.Point(392, 13);
            this.btnBrowseBoltwood.Name = "btnBrowseBoltwood";
            this.btnBrowseBoltwood.Size = new System.Drawing.Size(75, 23);
            this.btnBrowseBoltwood.TabIndex = 9;
            this.btnBrowseBoltwood.Text = "Browse";
            this.btnBrowseBoltwood.Click += new System.EventHandler(this.btnBrowseBoltwood_Click);
            // 
            // btnBrowseCumulus
            // 
            this.btnBrowseCumulus.Location = new System.Drawing.Point(392, 42);
            this.btnBrowseCumulus.Name = "btnBrowseCumulus";
            this.btnBrowseCumulus.Size = new System.Drawing.Size(75, 23);
            this.btnBrowseCumulus.TabIndex = 10;
            this.btnBrowseCumulus.Text = "Browse";
            this.btnBrowseCumulus.Click += new System.EventHandler(this.btnBrowseCumulus_Click);
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(268, 105);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 28);
            this.btnOK.TabIndex = 11;
            this.btnOK.Text = "OK";
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(372, 105);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 28);
            this.btnCancel.TabIndex = 12;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(46, 96);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(55, 13);
            this.label3.TabIndex = 15;
            this.label3.Text = "Max Wind";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(46, 130);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(70, 13);
            this.label4.TabIndex = 16;
            this.label4.Text = "Max Humidity";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(46, 160);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(54, 13);
            this.label5.TabIndex = 17;
            this.label5.Text = "Min Temp";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(46, 190);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(57, 13);
            this.label6.TabIndex = 18;
            this.label6.Text = "Max Temp";
            // 
            // pictureBox2
            // 
            this.pictureBox2.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox2.Image")));
            this.pictureBox2.Location = new System.Drawing.Point(286, 153);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(150, 25);
            this.pictureBox2.TabIndex = 20;
            this.pictureBox2.TabStop = false;
            // 
            // lblVersion
            // 
            this.lblVersion.AutoSize = true;
            this.lblVersion.Location = new System.Drawing.Point(302, 186);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(41, 13);
            this.lblVersion.TabIndex = 21;
            this.lblVersion.Text = "version";
            // 
            // SetupDialogForm
            // 
            this.ClientSize = new System.Drawing.Size(484, 217);
            this.Controls.Add(this.lblVersion);
            this.Controls.Add(this.pictureBox2);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtBoltwood);
            this.Controls.Add(this.txtCumulus);
            this.Controls.Add(this.txtMaxWind);
            this.Controls.Add(this.txtMaxHumidity);
            this.Controls.Add(this.txtMinTemp);
            this.Controls.Add(this.txtMaxTemp);
            this.Controls.Add(this.chkUseBoltwood);
            this.Controls.Add(this.chkUseCumulus);
            this.Controls.Add(this.chkEnableLogging);
            this.Controls.Add(this.btnBrowseBoltwood);
            this.Controls.Add(this.btnBrowseCumulus);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "SetupDialogForm";
            this.Text = "WeatherWatcher2 Setup";
            this.Load += new System.EventHandler(this.SetupDialogForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.Label lblVersion;
    }
}
