namespace server.Forms
{
    partial class Dashboard
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Dashboard));
            groupBox1 = new GroupBox();
            lblActiveClients = new Label();
            lblUptime = new Label();
            lblServerStatus = new Label();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(lblActiveClients);
            groupBox1.Controls.Add(lblUptime);
            groupBox1.Controls.Add(lblServerStatus);
            groupBox1.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            groupBox1.ForeColor = Color.Black;
            groupBox1.Location = new Point(10, 9);
            groupBox1.Margin = new Padding(3, 2, 3, 2);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new Padding(3, 2, 3, 2);
            groupBox1.Size = new Size(393, 166);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "SERVER STATUS";
            // 
            // lblActiveClients
            // 
            lblActiveClients.AutoSize = true;
            lblActiveClients.Font = new Font("Segoe UI", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblActiveClients.ForeColor = Color.Black;
            lblActiveClients.Location = new Point(19, 90);
            lblActiveClients.Name = "lblActiveClients";
            lblActiveClients.Size = new Size(103, 19);
            lblActiveClients.TabIndex = 2;
            lblActiveClients.Text = "Active clients: 0";
            // 
            // lblUptime
            // 
            lblUptime.AutoSize = true;
            lblUptime.Font = new Font("Segoe UI", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblUptime.ForeColor = Color.Black;
            lblUptime.Location = new Point(19, 64);
            lblUptime.Name = "lblUptime";
            lblUptime.Size = new Size(99, 19);
            lblUptime.TabIndex = 1;
            lblUptime.Text = "Uptime: 0m 0s";
            // 
            // lblServerStatus
            // 
            lblServerStatus.AutoSize = true;
            lblServerStatus.Font = new Font("Segoe UI", 13.8F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblServerStatus.ForeColor = Color.FromArgb(76, 175, 80);
            lblServerStatus.Location = new Point(19, 31);
            lblServerStatus.Name = "lblServerStatus";
            lblServerStatus.Size = new Size(89, 25);
            lblServerStatus.TabIndex = 0;
            lblServerStatus.Text = "Running";
            // 
            // Dashboard
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(890, 344);
            Controls.Add(groupBox1);
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(3, 2, 3, 2);
            Name = "Dashboard";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Dashboard";
            Load += Dashboard_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox groupBox1;
        private Label lblUptime;
        private Label lblServerStatus;
        private Label lblActiveClients;
    }
}