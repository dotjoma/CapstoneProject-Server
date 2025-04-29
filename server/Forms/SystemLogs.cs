using client.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace server.Forms
{
    public partial class SystemLogs : Form
    {
        public static SystemLogs _instance = new SystemLogs();

        public RichTextBox LogBox => _logBox;

        public SystemLogs()
        {
            InitializeComponent();

            _logBox.ReadOnly = true;
            _logBox.BackColor = Color.Black;
            _logBox.ForeColor = Color.Lime;
            _logBox.Font = new Font("Consolas", 10);
            _logBox.ScrollBars = RichTextBoxScrollBars.ForcedBoth;

            Logger.OnLogMessage += AppendLog;
        }

        public static SystemLogs Instance
        {
            get
            {
                if (_instance == null || _instance.IsDisposed)
                {
                    _instance = new SystemLogs();
                }
                return _instance;
            }
        }

        public void Clear()
        {
            if (_logBox.InvokeRequired)
            {
                _logBox.Invoke(new Action(Clear));
            }
            else
            {
                _logBox.Clear();
            }

            Logger.ClearLogs();
        }

        public void AppendLog(string message)
        {
            if (_logBox.InvokeRequired)
            {
                _logBox.Invoke(new Action(() => AppendLog(message)));
            }
            else
            {
                _logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
                _logBox.ScrollToCaret();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to clear the logs?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Clear();
            }
        }

        private void SystemLogs_Load(object sender, EventArgs e)
        {
            Logger.Initialize(_logBox);
        }
    }
}
