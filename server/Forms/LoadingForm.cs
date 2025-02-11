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
    public partial class LoadingForm : Form
    {
        public LoadingForm(string message)
        {
            InitializeComponent();

            // Form settings
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(300, 100);
            this.BackColor = Color.White;
            this.TopMost = true;  // Keep loading form on top

            // Create progress bar
            ProgressBar progressBar = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Size = new Size(250, 23),
                Location = new Point(25, 30)
            };
            this.Controls.Add(progressBar);

            // Create label
            Label label = new Label
            {
                Text = message,
                AutoSize = true,
                Location = new Point(25, 60)
            };
            this.Controls.Add(label);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x200;  // CS_NOCLOSE
                return cp;
            }
        }

        private void LoadingForm_Load(object sender, EventArgs e)
        {

        }
    }
}
