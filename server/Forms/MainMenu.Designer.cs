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
            label2 = new Label();
            richTextBox1 = new RichTextBox();
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
            btnStartServer.Location = new Point(12, 12);
            btnStartServer.Name = "btnStartServer";
            btnStartServer.Size = new Size(75, 23);
            btnStartServer.TabIndex = 1;
            btnStartServer.Text = "Start Server";
            btnStartServer.UseVisualStyleBackColor = true;
            btnStartServer.Click += btnStartServer_Click;
            // 
            // btnStopServer
            // 
            btnStopServer.Location = new Point(93, 12);
            btnStopServer.Name = "btnStopServer";
            btnStopServer.Size = new Size(75, 23);
            btnStopServer.TabIndex = 2;
            btnStopServer.Text = "Stop Server";
            btnStopServer.UseVisualStyleBackColor = true;
            btnStopServer.Click += btnStopServer_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 275);
            label1.Name = "label1";
            label1.Size = new Size(32, 15);
            label1.TabIndex = 4;
            label1.Text = "Logs";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 63);
            label2.Name = "label2";
            label2.Size = new Size(46, 15);
            label2.TabIndex = 5;
            label2.Text = "Client's";
            // 
            // richTextBox1
            // 
            richTextBox1.Location = new Point(12, 81);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(776, 176);
            richTextBox1.TabIndex = 7;
            richTextBox1.Text = "";
            // 
            // rtbLogs
            // 
            rtbLogs.Location = new Point(12, 293);
            rtbLogs.Name = "rtbLogs";
            rtbLogs.Size = new Size(776, 176);
            rtbLogs.TabIndex = 8;
            rtbLogs.Text = "";
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(12, 491);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(38, 15);
            lblStatus.TabIndex = 9;
            lblStatus.Text = "status";
            // 
            // button1
            // 
            button1.Location = new Point(699, 475);
            button1.Name = "button1";
            button1.Size = new Size(89, 23);
            button1.TabIndex = 10;
            button1.Text = "Clear Console";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // btnCloseWindow
            // 
            btnCloseWindow.Image = Properties.Resources.Close_Window;
            btnCloseWindow.Location = new Point(758, 5);
            btnCloseWindow.Name = "btnCloseWindow";
            btnCloseWindow.Size = new Size(30, 30);
            btnCloseWindow.SizeMode = PictureBoxSizeMode.StretchImage;
            btnCloseWindow.TabIndex = 11;
            btnCloseWindow.TabStop = false;
            btnCloseWindow.Click += btnCloseWindow_Click;
            // 
            // btnMinimizeWindow
            // 
            btnMinimizeWindow.Image = Properties.Resources.Minimize_Window;
            btnMinimizeWindow.Location = new Point(727, 5);
            btnMinimizeWindow.Name = "btnMinimizeWindow";
            btnMinimizeWindow.Size = new Size(30, 30);
            btnMinimizeWindow.SizeMode = PictureBoxSizeMode.StretchImage;
            btnMinimizeWindow.TabIndex = 12;
            btnMinimizeWindow.TabStop = false;
            btnMinimizeWindow.Click += btnMinimizeWindow_Click;
            // 
            // btnConnectToDB
            // 
            btnConnectToDB.Location = new Point(174, 12);
            btnConnectToDB.Name = "btnConnectToDB";
            btnConnectToDB.Size = new Size(174, 23);
            btnConnectToDB.TabIndex = 13;
            btnConnectToDB.Text = "Connect To Database";
            btnConnectToDB.UseVisualStyleBackColor = true;
            btnConnectToDB.Click += btnConnectToDB_Click;
            // 
            // MainMenu
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 639);
            Controls.Add(btnConnectToDB);
            Controls.Add(btnMinimizeWindow);
            Controls.Add(btnCloseWindow);
            Controls.Add(button1);
            Controls.Add(lblStatus);
            Controls.Add(rtbLogs);
            Controls.Add(richTextBox1);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(btnStopServer);
            Controls.Add(btnStartServer);
            FormBorderStyle = FormBorderStyle.None;
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
        private Label label2;
        private Button btnClose;
        private RichTextBox richTextBox1;
        private RichTextBox rtbLogs;
        private Label lblStatus;
        private Button button1;
        private PictureBox btnCloseWindow;
        private PictureBox btnMinimizeWindow;
        private Button btnConnectToDB;
    }
}