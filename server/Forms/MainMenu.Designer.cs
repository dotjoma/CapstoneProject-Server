namespace server.Forms
{
    partial class MainMenu
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
            btnStartServer = new Button();
            btnStopServer = new Button();
            label1 = new Label();
            rtbLogs = new RichTextBox();
            lblStatus = new Label();
            button1 = new Button();
            btnCloseWindow = new PictureBox();
            btnMinimizeWindow = new PictureBox();
            btnConnectToDB = new Button();
            ((System.ComponentModel.ISupportInitialize)btnCloseWindow).BeginInit();
            ((System.ComponentModel.ISupportInitialize)btnMinimizeWindow).BeginInit();
            SuspendLayout();
            // 
            // btnStartServer
            // 
            btnStartServer.Location = new Point(14, 16);
            btnStartServer.Margin = new Padding(3, 4, 3, 4);
            btnStartServer.Name = "btnStartServer";
            btnStartServer.Size = new Size(106, 31);
            btnStartServer.TabIndex = 1;
            btnStartServer.Text = "Start Server";
            btnStartServer.UseVisualStyleBackColor = true;
            btnStartServer.Click += btnStartServer_Click;
            // 
            // btnStopServer
            // 
            btnStopServer.Location = new Point(126, 16);
            btnStopServer.Margin = new Padding(3, 4, 3, 4);
            btnStopServer.Name = "btnStopServer";
            btnStopServer.Size = new Size(104, 31);
            btnStopServer.TabIndex = 2;
            btnStopServer.Text = "Stop Server";
            btnStopServer.UseVisualStyleBackColor = true;
            btnStopServer.Click += btnStopServer_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.Location = new Point(12, 65);
            label1.Name = "label1";
            label1.Size = new Size(55, 28);
            label1.TabIndex = 4;
            label1.Text = "Logs";
            // 
            // rtbLogs
            // 
            rtbLogs.BorderStyle = BorderStyle.FixedSingle;
            rtbLogs.Location = new Point(12, 97);
            rtbLogs.Margin = new Padding(3, 4, 3, 4);
            rtbLogs.Name = "rtbLogs";
            rtbLogs.Size = new Size(886, 352);
            rtbLogs.TabIndex = 8;
            rtbLogs.Text = "";
            // 
            // lblStatus
            // 
            lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblStatus.AutoSize = true;
            lblStatus.FlatStyle = FlatStyle.System;
            lblStatus.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblStatus.Location = new Point(14, 509);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(66, 28);
            lblStatus.TabIndex = 9;
            lblStatus.Text = "status";
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            button1.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button1.Location = new Point(723, 498);
            button1.Margin = new Padding(3, 4, 3, 4);
            button1.Name = "button1";
            button1.Size = new Size(175, 39);
            button1.TabIndex = 10;
            button1.Text = "Clear Console";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // btnCloseWindow
            // 
            btnCloseWindow.Image = Properties.Resources.Close_Window;
            btnCloseWindow.Location = new Point(866, 7);
            btnCloseWindow.Margin = new Padding(3, 4, 3, 4);
            btnCloseWindow.Name = "btnCloseWindow";
            btnCloseWindow.Size = new Size(34, 40);
            btnCloseWindow.SizeMode = PictureBoxSizeMode.StretchImage;
            btnCloseWindow.TabIndex = 11;
            btnCloseWindow.TabStop = false;
            btnCloseWindow.Click += btnCloseWindow_Click;
            // 
            // btnMinimizeWindow
            // 
            btnMinimizeWindow.Image = Properties.Resources.Minimize_Window;
            btnMinimizeWindow.Location = new Point(831, 7);
            btnMinimizeWindow.Margin = new Padding(3, 4, 3, 4);
            btnMinimizeWindow.Name = "btnMinimizeWindow";
            btnMinimizeWindow.Size = new Size(34, 40);
            btnMinimizeWindow.SizeMode = PictureBoxSizeMode.StretchImage;
            btnMinimizeWindow.TabIndex = 12;
            btnMinimizeWindow.TabStop = false;
            btnMinimizeWindow.Click += btnMinimizeWindow_Click;
            // 
            // btnConnectToDB
            // 
            btnConnectToDB.Location = new Point(236, 16);
            btnConnectToDB.Margin = new Padding(3, 4, 3, 4);
            btnConnectToDB.Name = "btnConnectToDB";
            btnConnectToDB.Size = new Size(199, 31);
            btnConnectToDB.TabIndex = 13;
            btnConnectToDB.Text = "Connect To Database";
            btnConnectToDB.UseVisualStyleBackColor = true;
            btnConnectToDB.Click += btnConnectToDB_Click;
            // 
            // MainMenu
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(914, 556);
            Controls.Add(btnConnectToDB);
            Controls.Add(btnMinimizeWindow);
            Controls.Add(btnCloseWindow);
            Controls.Add(button1);
            Controls.Add(lblStatus);
            Controls.Add(rtbLogs);
            Controls.Add(label1);
            Controls.Add(btnStopServer);
            Controls.Add(btnStartServer);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(3, 4, 3, 4);
            Name = "MainMenu";
            StartPosition = FormStartPosition.CenterScreen;
            Load += MainMenu_Load;
            MouseDown += MainMenu_MouseDown;
            MouseMove += MainMenu_MouseMove;
            MouseUp += MainMenu_MouseUp;
            ((System.ComponentModel.ISupportInitialize)btnCloseWindow).EndInit();
            ((System.ComponentModel.ISupportInitialize)btnMinimizeWindow).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Button btnStartServer;
        private Button btnStopServer;
        private Label label1;
        private RichTextBox rtbLogs;
        private Label lblStatus;
        private Button button1;
        private PictureBox btnCloseWindow;
        private PictureBox btnMinimizeWindow;
        private Button btnConnectToDB;
    }
}