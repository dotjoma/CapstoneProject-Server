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
            groupBox1.Location = new Point(12, 12);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(449, 221);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "SERVER STATUS";
            // 
            // lblActiveClients
            // 
            lblActiveClients.AutoSize = true;
            lblActiveClients.Font = new Font("Segoe UI", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblActiveClients.ForeColor = Color.Black;
            lblActiveClients.Location = new Point(22, 120);
            lblActiveClients.Name = "lblActiveClients";
            lblActiveClients.Size = new Size(127, 23);
            lblActiveClients.TabIndex = 2;
            lblActiveClients.Text = "Active clients: 0";
            // 
            // lblUptime
            // 
            lblUptime.AutoSize = true;
            lblUptime.Font = new Font("Segoe UI", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblUptime.ForeColor = Color.Black;
            lblUptime.Location = new Point(22, 86);
            lblUptime.Name = "lblUptime";
            lblUptime.Size = new Size(120, 23);
            lblUptime.TabIndex = 1;
            lblUptime.Text = "Uptime: 0m 0s";
            // 
            // lblServerStatus
            // 
            lblServerStatus.AutoSize = true;
            lblServerStatus.Font = new Font("Segoe UI", 13.8F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblServerStatus.ForeColor = Color.FromArgb(76, 175, 80);
            lblServerStatus.Location = new Point(22, 41);
            lblServerStatus.Name = "lblServerStatus";
            lblServerStatus.Size = new Size(106, 31);
            lblServerStatus.TabIndex = 0;
            lblServerStatus.Text = "Running";
            // 
            // Dashboard
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(1017, 458);
            Controls.Add(groupBox1);
            FormBorderStyle = FormBorderStyle.None;
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