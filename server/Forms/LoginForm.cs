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

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Username and password cannot be empty.", "Login Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string? sessionToken = _authController.ServerLogin(username, password);

                switch (sessionToken)
                {
                    case null:
                        MessageBox.Show("Invalid username or password.", "Login Failed",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;

                    case "ALREADY_LOGGED_IN":
                        MessageBox.Show("This account has an active session. Please wait 3 minutes.", "Login Denied",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;

                    default:
                        SessionManager.Instance.CurrentSessionToken = sessionToken;
                        SessionManager.Instance.CurrentUsername = username;

                        var mainfrm = new MainMenu();
                        mainfrm.Show();

                        if (cbStartServerOnLogin.Checked)
                            StartServerOnLogin?.Invoke();

                        this.Hide();
                        break;
                }
            }
            catch (InvalidOperationException ex)
            {
                // Handle specific error when login fails due to invalid username/password or other errors
                MessageBox.Show($"Login failed: {ex.Message}", "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                // Handle other exceptions (e.g., network issues)
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
