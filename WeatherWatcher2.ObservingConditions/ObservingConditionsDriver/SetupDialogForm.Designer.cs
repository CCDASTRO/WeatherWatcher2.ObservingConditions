namespace WeatherWatcher2.ObservingConditions
{
    partial class SetupDialogForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TextBox txtBoltwood;
        private System.Windows.Forms.TextBox txtCumulus;
        private System.Windows.Forms.TextBox txtMaxWind;
        private System.Windows.Forms.TextBox txtMaxHumidity;
        private System.Windows.Forms.TextBox txtMinTemp;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;

        private void InitializeComponent()
        {
            this.txtBoltwood = new System.Windows.Forms.TextBox();
            this.txtCumulus = new System.Windows.Forms.TextBox();
            this.txtMaxWind = new System.Windows.Forms.TextBox();
            this.txtMaxHumidity = new System.Windows.Forms.TextBox();
            this.txtMinTemp = new System.Windows.Forms.TextBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // txtBoltwood
            // 
            this.txtBoltwood.Location = new System.Drawing.Point(154, 51);
            this.txtBoltwood.Name = "txtBoltwood";
            this.txtBoltwood.Size = new System.Drawing.Size(100, 20);
            this.txtBoltwood.TabIndex = 0;
            // 
            // txtCumulus
            // 
            this.txtCumulus.Location = new System.Drawing.Point(154, 89);
            this.txtCumulus.Name = "txtCumulus";
            this.txtCumulus.Size = new System.Drawing.Size(100, 20);
            this.txtCumulus.TabIndex = 1;
            // 
            // txtMaxWind
            // 
            this.txtMaxWind.Location = new System.Drawing.Point(154, 115);
            this.txtMaxWind.Name = "txtMaxWind";
            this.txtMaxWind.Size = new System.Drawing.Size(100, 20);
            this.txtMaxWind.TabIndex = 2;
            // 
            // txtMaxHumidity
            // 
            this.txtMaxHumidity.Location = new System.Drawing.Point(154, 141);
            this.txtMaxHumidity.Name = "txtMaxHumidity";
            this.txtMaxHumidity.Size = new System.Drawing.Size(100, 20);
            this.txtMaxHumidity.TabIndex = 3;
            // 
            // txtMinTemp
            // 
            this.txtMinTemp.Location = new System.Drawing.Point(154, 167);
            this.txtMinTemp.Name = "txtMinTemp";
            this.txtMinTemp.Size = new System.Drawing.Size(100, 20);
            this.txtMinTemp.TabIndex = 4;
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(0, 29);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 5;
            this.btnOK.Text = "OK";
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(0, 0);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // SetupDialogForm
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.txtBoltwood);
            this.Controls.Add(this.txtCumulus);
            this.Controls.Add(this.txtMaxWind);
            this.Controls.Add(this.txtMaxHumidity);
            this.Controls.Add(this.txtMinTemp);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.Name = "SetupDialogForm";
            this.Text = "WeatherWatcher2 Setup";
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}