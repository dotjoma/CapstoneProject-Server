using server.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace server.Forms
{
    public partial class Dashboard : Form
    {
        private readonly Stopwatch _uptimeStopwatch = new Stopwatch();
        private readonly System.Windows.Forms.Timer _uptimeTimer = new System.Windows.Forms.Timer { Interval = 1000 };

        public static Dashboard _instance = new Dashboard();

        public Dashboard()
        {
            InitializeComponent();
            _uptimeTimer.Interval = 1000;
            _uptimeTimer.Tick += uptimeTimer_Tick;
            MainMenu.OnServerStarted += OnServerStarted;
            MainMenu.OnServerStopped += OnServerStopped;
            MainMenu.OnLogOut += OnServerStopped;
        }

        public static Dashboard Instance
        {
            get
            {
                if (_instance == null || _instance.IsDisposed)
                {
                    _instance = new Dashboard();
                }
                return _instance;
            }
        }

        private void Dashboard_Load(object sender, EventArgs e)
        {
            lblServerStatus.Text = ServerStatus.Instance.CurrentStatus;
        }

        private void OnServerStarted()
        {
            _uptimeStopwatch.Restart();
            _uptimeTimer.Start();
            lblServerStatus.Text = "Running";
        }

        private void OnServerStopped()
        {
            _uptimeStopwatch.Stop();
            _uptimeTimer.Stop();
            lblServerStatus.Text = "Stopped";
            lblUptime.Text = "Uptime: 0m 0s";
        }

        private void uptimeTimer_Tick(object? sender, EventArgs e)
        {
            TimeSpan elapsed = _uptimeStopwatch.Elapsed;

            string formattedUptime = elapsed.TotalHours >= 1
                ? $"Uptime: {elapsed.Hours}h {elapsed.Minutes}m"
                : $"Uptime: {elapsed.Minutes}m {elapsed.Seconds}s";

            if (lblUptime.InvokeRequired)
            {
                lblUptime.BeginInvoke(new Action(() => lblUptime.Text = formattedUptime));
            }
            else
            {
                lblUptime.Text = formattedUptime;
            }
        }
    }
}
