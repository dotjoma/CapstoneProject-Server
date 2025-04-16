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
    public partial class ReAuthForm : Form
    {
        AuthController _authController = new AuthController();

        public static event Action? StartServerOnLogin;

        public ReAuthForm()
        {
            InitializeComponent();
            this.ShowInTaskbar = false;
        }

        private void ReAuthForm_Load(object sender, EventArgs e)
        {

        }

        private void btnContinueSession_Click(object sender, EventArgs e)
        {
            string? username = SessionManager.Instance.CurrentUsername;
            string? password = txtPassword.Text.Trim();

            if (username == null || string.IsNullOrEmpty(username))
            {
                MessageBox.Show("An unexpected error occurred. Please log out and try again", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("The password field is empty", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string? response = _authController.ServerLogin(username, password);

                switch (response)
                {
                    case null:
                        //MessageBox.Show("Incorrect password.", "Authentication Failed",
                        //    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;

                    case "ALREADY_LOGGED_IN":
                        MessageBox.Show("This account has an active session!", "Authentication Denied",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;

                    default:
                        SessionManager.Instance.CurrentSessionToken = response;
                        StartServerOnLogin?.Invoke();
                        this.Dispose();
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during login: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {

        }
    }
}
