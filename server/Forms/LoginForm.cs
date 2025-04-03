using client.Helpers;
using server.Controllers;
using server.Services;
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
    public partial class LoginForm : Form
    {
        AuthController _authController = new AuthController();

        public static event Action? StartServerOnLogin;
        public LoginForm()
        {
            InitializeComponent();
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            Logger.Write("LOGIN_STARTED", "Server login started");
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();

            try
            {
                string? sessionToken = _authController.ServerLogin(username, password);

                if (string.IsNullOrEmpty(sessionToken))
                {
                    MessageBox.Show("Invalid username or password.", "Login Failed",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                SessionManager.Instance.CurrentSessionToken = sessionToken;

                var mainfrm = new MainMenu();
                mainfrm.Show();

                if (cbStartServerOnLogin.Checked)
                    StartServerOnLogin?.Invoke();

                this.Hide();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to login: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }
    }
}
