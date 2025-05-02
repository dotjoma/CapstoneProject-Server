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

        private bool isSigningIn = false;

        public static event Action? StartServerOnLogin;
        public LoginForm()
        {
            InitializeComponent();
            this.KeyPreview = true;
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            Logger.Write("LOGIN_STARTED", "Server login started");
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            isSigningIn = true;
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

                if (sessionToken == null)
                {
                    MessageBox.Show("Invalid username or password.", "Login Failed",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                else if (sessionToken == "ALREADY_LOGGED_IN")
                {
                    MessageBox.Show("This account has an active session. Please wait 3 minutes.", "Login Denied",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                else
                {
                    SessionManager.Instance.CurrentSessionToken = sessionToken;
                    SessionManager.Instance.CurrentUsername = username;

                    this.Hide();
                    new MainMenu().Show();

                    if (cbStartServerOnLogin.Checked)
                        StartServerOnLogin?.Invoke();
                }
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show($"Login failed: {ex.Message}", "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isSigningIn = false;
                GC.Collect();
            }
        }

        private void LoginForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (!isSigningIn)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    btnLogin.PerformClick();
                }
            }
        }
    }
}
