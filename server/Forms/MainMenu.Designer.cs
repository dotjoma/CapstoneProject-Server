﻿namespace server.Forms
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainMenu));
            btnStartServer = new Button();
            btnStopServer = new Button();
            lblStatus = new Label();
            btnConnectToDB = new Button();
            panel1 = new Panel();
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            txSettings = new ToolStripMenuItem();
            tsLogout = new ToolStripMenuItem();
            tsExit = new ToolStripMenuItem();
            monitorToolStripMenuItem = new ToolStripMenuItem();
            reportsToolStripMenuItem = new ToolStripMenuItem();
            toolsToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            panel2 = new Panel();
            buttonPanel = new Panel();
            btnSystemLogs = new Button();
            btnConnectedClients = new Button();
            btnDashboard = new Button();
            pnlContainer = new Panel();
            panel1.SuspendLayout();
            menuStrip1.SuspendLayout();
            panel2.SuspendLayout();
            buttonPanel.SuspendLayout();
            SuspendLayout();
            // 
            // btnStartServer
            // 
            btnStartServer.BackColor = Color.FromArgb(76, 175, 80);
            btnStartServer.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnStartServer.ForeColor = Color.White;
            btnStartServer.Location = new Point(12, 7);
            btnStartServer.Margin = new Padding(3, 4, 3, 4);
            btnStartServer.Name = "btnStartServer";
            btnStartServer.Size = new Size(131, 45);
            btnStartServer.TabIndex = 1;
            btnStartServer.Text = "Start Server";
            btnStartServer.UseVisualStyleBackColor = false;
            btnStartServer.Click += btnStartServer_Click;
            // 
            // btnStopServer
            // 
            btnStopServer.BackColor = Color.FromArgb(244, 67, 54);
            btnStopServer.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold);
            btnStopServer.ForeColor = Color.White;
            btnStopServer.Location = new Point(149, 7);
            btnStopServer.Margin = new Padding(3, 4, 3, 4);
            btnStopServer.Name = "btnStopServer";
            btnStopServer.Size = new Size(130, 45);
            btnStopServer.TabIndex = 2;
            btnStopServer.Text = "Stop Server";
            btnStopServer.UseVisualStyleBackColor = false;
            btnStopServer.Click += btnStopServer_Click;
            // 
            // lblStatus
            // 
            lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            lblStatus.AutoSize = true;
            lblStatus.FlatStyle = FlatStyle.System;
            lblStatus.Font = new Font("Arial", 10.2F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblStatus.Location = new Point(8, 17);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(112, 19);
            lblStatus.TabIndex = 9;
            lblStatus.Text = "Server Status:";
            // 
            // btnConnectToDB
            // 
            btnConnectToDB.Location = new Point(806, 6);
            btnConnectToDB.Margin = new Padding(3, 4, 3, 4);
            btnConnectToDB.Name = "btnConnectToDB";
            btnConnectToDB.Size = new Size(199, 40);
            btnConnectToDB.TabIndex = 13;
            btnConnectToDB.Text = "Connect To Database";
            btnConnectToDB.UseVisualStyleBackColor = true;
            btnConnectToDB.Click += btnConnectToDB_Click;
            // 
            // panel1
            // 
            panel1.BackColor = Color.FromArgb(208, 208, 208);
            panel1.Controls.Add(btnStartServer);
            panel1.Controls.Add(btnStopServer);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 31);
            panel1.Name = "panel1";
            panel1.Size = new Size(1017, 58);
            panel1.TabIndex = 14;
            // 
            // menuStrip1
            // 
            menuStrip1.BackColor = Color.FromArgb(224, 224, 224);
            menuStrip1.Font = new Font("Segoe UI Semibold", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            menuStrip1.ImageScalingSize = new Size(20, 20);
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, monitorToolStripMenuItem, reportsToolStripMenuItem, toolsToolStripMenuItem, helpToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1017, 31);
            menuStrip1.TabIndex = 15;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { txSettings, tsLogout, tsExit });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(50, 27);
            fileToolStripMenuItem.Text = "File";
            fileToolStripMenuItem.Click += fileToolStripMenuItem_Click;
            // 
            // txSettings
            // 
            txSettings.Name = "txSettings";
            txSettings.Size = new Size(155, 28);
            txSettings.Text = "Settings";
            // 
            // tsLogout
            // 
            tsLogout.Name = "tsLogout";
            tsLogout.Size = new Size(155, 28);
            tsLogout.Text = "Logout";
            tsLogout.Click += tsLogout_Click;
            // 
            // tsExit
            // 
            tsExit.Name = "tsExit";
            tsExit.Size = new Size(155, 28);
            tsExit.Text = "Exit";
            // 
            // monitorToolStripMenuItem
            // 
            monitorToolStripMenuItem.Name = "monitorToolStripMenuItem";
            monitorToolStripMenuItem.Size = new Size(86, 27);
            monitorToolStripMenuItem.Text = "Monitor";
            // 
            // reportsToolStripMenuItem
            // 
            reportsToolStripMenuItem.Name = "reportsToolStripMenuItem";
            reportsToolStripMenuItem.Size = new Size(83, 27);
            reportsToolStripMenuItem.Text = "Reports";
            // 
            // toolsToolStripMenuItem
            // 
            toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            toolsToolStripMenuItem.Size = new Size(62, 27);
            toolsToolStripMenuItem.Text = "Tools";
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(60, 27);
            helpToolStripMenuItem.Text = "Help";
            // 
            // panel2
            // 
            panel2.BackColor = Color.FromArgb(224, 224, 224);
            panel2.Controls.Add(btnConnectToDB);
            panel2.Controls.Add(lblStatus);
            panel2.Dock = DockStyle.Bottom;
            panel2.Location = new Point(0, 603);
            panel2.Name = "panel2";
            panel2.Size = new Size(1017, 52);
            panel2.TabIndex = 16;
            // 
            // buttonPanel
            // 
            buttonPanel.BackColor = Color.FromArgb(224, 224, 224);
            buttonPanel.Controls.Add(btnSystemLogs);
            buttonPanel.Controls.Add(btnConnectedClients);
            buttonPanel.Controls.Add(btnDashboard);
            buttonPanel.Dock = DockStyle.Top;
            buttonPanel.Location = new Point(0, 89);
            buttonPanel.Name = "buttonPanel";
            buttonPanel.Size = new Size(1017, 56);
            buttonPanel.TabIndex = 18;
            // 
            // btnSystemLogs
            // 
            btnSystemLogs.BackColor = Color.Transparent;
            btnSystemLogs.Dock = DockStyle.Left;
            btnSystemLogs.FlatAppearance.BorderSize = 0;
            btnSystemLogs.FlatAppearance.MouseOverBackColor = Color.LightGray;
            btnSystemLogs.FlatStyle = FlatStyle.Flat;
            btnSystemLogs.Location = new Point(286, 0);
            btnSystemLogs.Name = "btnSystemLogs";
            btnSystemLogs.Size = new Size(143, 56);
            btnSystemLogs.TabIndex = 2;
            btnSystemLogs.Text = "System Logs";
            btnSystemLogs.UseVisualStyleBackColor = false;
            btnSystemLogs.Click += btnSystemLogs_Click;
            // 
            // btnConnectedClients
            // 
            btnConnectedClients.BackColor = Color.Transparent;
            btnConnectedClients.Dock = DockStyle.Left;
            btnConnectedClients.FlatAppearance.BorderSize = 0;
            btnConnectedClients.FlatAppearance.MouseOverBackColor = Color.LightGray;
            btnConnectedClients.FlatStyle = FlatStyle.Flat;
            btnConnectedClients.Location = new Point(143, 0);
            btnConnectedClients.Name = "btnConnectedClients";
            btnConnectedClients.Size = new Size(143, 56);
            btnConnectedClients.TabIndex = 1;
            btnConnectedClients.Text = "Connected Clients";
            btnConnectedClients.UseVisualStyleBackColor = false;
            btnConnectedClients.Click += btnConnectedClients_Click;
            // 
            // btnDashboard
            // 
            btnDashboard.BackColor = Color.White;
            btnDashboard.Dock = DockStyle.Left;
            btnDashboard.FlatAppearance.BorderSize = 0;
            btnDashboard.FlatAppearance.MouseOverBackColor = Color.LightGray;
            btnDashboard.FlatStyle = FlatStyle.Flat;
            btnDashboard.Location = new Point(0, 0);
            btnDashboard.Name = "btnDashboard";
            btnDashboard.Size = new Size(143, 56);
            btnDashboard.TabIndex = 0;
            btnDashboard.Text = "Dashboard";
            btnDashboard.UseVisualStyleBackColor = false;
            btnDashboard.Click += btnDashboard_Click;
            // 
            // pnlContainer
            // 
            pnlContainer.Dock = DockStyle.Fill;
            pnlContainer.Location = new Point(0, 145);
            pnlContainer.Name = "pnlContainer";
            pnlContainer.Size = new Size(1017, 458);
            pnlContainer.TabIndex = 19;
            // 
            // MainMenu
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(1017, 655);
            Controls.Add(pnlContainer);
            Controls.Add(buttonPanel);
            Controls.Add(panel1);
            Controls.Add(menuStrip1);
            Controls.Add(panel2);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip1;
            Margin = new Padding(3, 4, 3, 4);
            Name = "MainMenu";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "ELICIAS GARDEN FOOD PARK";
            FormClosing += MainMenu_FormClosing;
            Load += MainMenu_Load;
            MouseDown += MainMenu_MouseDown;
            MouseMove += MainMenu_MouseMove;
            MouseUp += MainMenu_MouseUp;
            panel1.ResumeLayout(false);
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            buttonPanel.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Button btnStartServer;
        private Button btnStopServer;
        private Label lblStatus;
        private Button btnConnectToDB;
        private Panel panel1;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem monitorToolStripMenuItem;
        private ToolStripMenuItem reportsToolStripMenuItem;
        private ToolStripMenuItem toolsToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private Panel panel2;
        private Panel buttonPanel;
        private Button btnSystemLogs;
        private Button btnConnectedClients;
        private Button btnDashboard;
        private Panel pnlContainer;
        private ToolStripMenuItem txSettings;
        private ToolStripMenuItem tsLogout;
        private ToolStripMenuItem tsExit;
    }
}