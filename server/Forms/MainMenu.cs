using server.Core.Network;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using server.Database;
using System.Net;
using System.Diagnostics;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Cmp;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;

namespace server.Forms
{
    public partial class MainMenu : Form
    {
        private TcpListener? listener;
        private bool isServerRunning;
        private Dictionary<string, string> users = new Dictionary<string, string>();
        private int serverPort = 8888;

        private Point dragOffset;
        private bool isDragging = false;

        private readonly string connectionString = "Server=localhost;Database=elicias;Uid=root;Pwd=;";

        public MainMenu()
        {
            InitializeComponent();
            SetupForm();
        }

        private void SetupForm()
        {
            btnStartServer.Enabled = true;
            btnStopServer.Enabled = false;
            rtbLogs.ReadOnly = true;
            UpdateStatus("Server stopped");
        }





        private async void btnStartServer_Click(object? sender, EventArgs e)
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, serverPort);
                listener.Start();
                isServerRunning = true;

                LogMessage($"Server started on port {serverPort}");
                UpdateStatus("Server running");

                btnStartServer.Enabled = false;
                btnStopServer.Enabled = true;

                await ListenForClients();
            }
            catch (Exception ex)
            {
                LogMessage($"Error starting server: {ex.Message}");
                StopServer();
            }
        }

        private void btnStopServer_Click(object? sender, EventArgs e)
        {
            ForceStopServer();
        }

        private void MainMenu_Load(object sender, EventArgs e)
        {
            LogMessage("Server application started");

            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    LogMessage("Database connection successful");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Database connection error: {ex.Message}");
                MessageBox.Show("Could not connect to database. Please check your connection settings.",
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MainMenu_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!isServerRunning) return;

            switch (e.CloseReason)
            {
                case CloseReason.UserClosing:
                    var result = MessageBox.Show(
                        "The server is still running. Do you want to stop the server and exit?",
                        "Server Running",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        StopServer();
                    }
                    else
                    {
                        e.Cancel = true; // Prevent form from closing
                    }
                    break;

                case CloseReason.WindowsShutDown:
                    MessageBox.Show(
                        "Windows is shutting down. The server will be stopped automatically.",
                        "Windows Shutdown",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    StopServer();
                    break;

                case CloseReason.TaskManagerClosing:
                    MessageBox.Show(
                        "Application is being closed by Task Manager. The server will be stopped.",
                        "Task Manager Closing",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    StopServer();
                    break;

                case CloseReason.ApplicationExitCall:
                    var confirmExit = MessageBox.Show(
                        "Are you sure you want to stop the server and exit the application?",
                        "Confirm Exit",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (confirmExit == DialogResult.Yes)
                    {
                        StopServer();
                    }
                    else
                    {
                        e.Cancel = true;
                    }
                    break;

                default:
                    var defaultResult = MessageBox.Show(
                        "The server is still running. Would you like to stop it before closing?",
                        "Server Running",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (defaultResult == DialogResult.Yes)
                    {
                        StopServer();
                    }
                    else
                    {
                        e.Cancel = true;
                    }
                    break;
            }

            // Log the closing event
            LogMessage($"Application closing: {e.CloseReason}");
        }

        private void StopServer()
        {
            if (isServerRunning && MessageBox.Show("Are you sure you want to stop the server?", "Stop Server",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    isServerRunning = false;
                    listener?.Stop();
                    listener = null;
                    LogMessage("Server stopped");
                    UpdateStatus("Server stopped");
                }
                catch (Exception ex)
                {
                    LogMessage($"Error stopping server during shutdown: {ex.Message}");
                }
                finally
                {
                    btnStartServer.Enabled = true;
                    btnStopServer.Enabled = false;
                }
            }
        }

        private bool StopServerWithConfirmation()
        {
            if (!isServerRunning) return true;

            var result = MessageBox.Show(
                "Are you sure you want to stop the server and exit?",
                "Stop Server",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                ForceStopServer();
                return true;
            }

            return false;
        }

        private void ForceStopServer()
        {
            try
            {
                if (isServerRunning)
                {
                    isServerRunning = false;
                    listener?.Stop();
                    listener = null;
                    LogMessage("Server stopped");
                    UpdateStatus("Server stopped");

                    btnStartServer.Enabled = true;
                    btnStopServer.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error stopping server: {ex.Message}");
            }
        }


        private async Task ListenForClients()
        {
            if (listener == null)
            {
                LogMessage("Error: TCP listener is not initialized");
                return;
            }

            while (isServerRunning && listener != null)
            {
                try
                {
                    var client = await listener.AcceptTcpClientAsync();
                    if (client != null)
                    {
                        _ = HandleClientAsync(client);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Server was stopped, exit gracefully
                    break;
                }
                catch (Exception ex)
                {
                    if (isServerRunning) // Only log if it wasn't a normal shutdown
                    {
                        LogMessage($"Error accepting client: {ex.Message}");
                    }
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                var endpoint = client.Client.RemoteEndPoint as IPEndPoint;
                string clientAddress = endpoint?.Address.ToString() ?? "Unknown";
                LogMessage($"Client connected from: {clientAddress}");

                using (client)
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string jsonRequest = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    LogMessage($"Received from {clientAddress}: {jsonRequest}");

                    var request = JsonConvert.DeserializeObject<Packet>(jsonRequest);
                    if (request == null)
                    {
                        LogMessage($"Error: Invalid packet received from {clientAddress}");
                        return;
                    }

                    var response = ProcessRequest(request);
                    string jsonResponse = JsonConvert.SerializeObject(response);
                    byte[] responseData = Encoding.UTF8.GetBytes(jsonResponse);
                    await stream.WriteAsync(responseData, 0, responseData.Length);

                    LogMessage($"Sent to {clientAddress}: {jsonResponse}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error handling client: {ex.Message}");
            }
        }

        private Packet ProcessRequest(Packet request)
        {
            LogMessage($"Processing request type: {request.Type}");

            switch (request.Type)
            {
                case PacketType.Register:
                    return HandleRegistration(request);
                case PacketType.RegisterResponse:
                    LogMessage("Received RegisterResponse packet type");
                    return HandleRegistration(request);
                default:
                    LogMessage($"Unknown packet type: {request.Type}");
                    return new Packet
                    {
                        Type = PacketType.RegisterResponse,
                        Data = new Dictionary<string, string>
                        {
                            { "success", "false" },
                            { "message", $"Unknown request type: {request.Type}" }
                        }
                    };
            }
        }

        private Packet HandleRegistration(Packet request)
        {
            try
            {
                LogMessage($"Processing registration for username: {request.Data["username"]}");

                if (!request.Data.ContainsKey("username") || !request.Data.ContainsKey("password"))
                {
                    LogMessage("Registration failed: Missing username or password");
                    return new Packet
                    {
                        Type = PacketType.RegisterResponse,
                        Success = false,
                        Message = "Username and password are required",
                        Data = new Dictionary<string, string>
                        {
                            { "success", "false" },
                            { "message", "Username and password are required" }
                        }
                    };
                }

                string username = request.Data["username"];
                string password = request.Data["password"];

                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    LogMessage("Database connection opened");

                    // Check if username exists
                    string checkQuery = "SELECT COUNT(*) FROM users WHERE username = @username";
                    using (var checkCommand = new MySqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@username", username);
                        int userCount = Convert.ToInt32(checkCommand.ExecuteScalar());

                        if (userCount > 0)
                        {
                            LogMessage($"Registration failed: Username {username} already exists");
                            return new Packet
                            {
                                Type = PacketType.RegisterResponse,
                                Success = false,
                                Message = "Username already exists",
                                Data = new Dictionary<string, string>
                                {
                                    { "success", "false" },
                                    { "message", "Username already exists" }
                                }
                            };
                        }
                    }

                    // Insert new user
                    string insertQuery = @"
                INSERT INTO users (username, password, created_at) 
                VALUES (@username, @password, @created_at)";

                    using (var insertCommand = new MySqlCommand(insertQuery, connection))
                    {
                        insertCommand.Parameters.AddWithValue("@username", username);
                        insertCommand.Parameters.AddWithValue("@password", HashPassword(password));
                        insertCommand.Parameters.AddWithValue("@created_at", DateTime.Now);

                        insertCommand.ExecuteNonQuery();
                    }

                    LogMessage($"Successfully registered new user: {username}");
                    return new Packet
                    {
                        Type = PacketType.RegisterResponse,
                        Success = true,
                        Message = "Registration successful",
                        Data = new Dictionary<string, string>
                        {
                            { "success", "true" },
                            { "message", "Registration successful" }
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Registration error: {ex.Message}");
                return new Packet
                {
                    Type = PacketType.RegisterResponse,
                    Success = false,
                    Message = "Internal server error",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", "Internal server error" }
                    }
                };
            }
        }

        // Add this helper method for password hashing
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        private void LogMessage(string message)
        {
            if (rtbLogs.InvokeRequired)
            {
                rtbLogs.Invoke(new Action(() => LogMessage(message)));
                return;
            }

            rtbLogs.AppendText($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
            rtbLogs.ScrollToCaret();
        }

        private void UpdateStatus(string status)
        {
            if (lblStatus.InvokeRequired)
            {
                lblStatus.Invoke(new Action(() => UpdateStatus(status)));
                return;
            }

            lblStatus.Text = $"Status: {status}";
        }

        private void MainMenu_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isDragging = true;
                dragOffset = new Point(e.X, e.Y);
            }
        }

        private void MainMenu_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point newLocation = PointToScreen(new Point(e.X, e.Y));
                Location = new Point(newLocation.X - dragOffset.X, newLocation.Y - dragOffset.Y);
            }
        }

        private void MainMenu_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            if (StopServerWithConfirmation())
            {
                LogMessage("Server stopped by user request");
            }
            Application.Exit();
        }
    }
}
