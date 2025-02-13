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
using client.Helpers;
using System.Collections;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Net.Http;
using Org.BouncyCastle.Tls;

namespace server.Forms
{
    public partial class MainMenu : Form
    {
        private TcpListener? listener;
        private bool isServerRunning;
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
                string clientIp = GetClientIpAddress(client);
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

                    var response = ProcessRequest(request, client);
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

        private Packet ProcessRequest(Packet request, TcpClient tcpClient)
        {
            LogMessage($"Processing request type: {request.Type}");
            Logger.Write("CLIENT REQUEST", $"Processing request type: {request.Type}");

            switch (request.Type)
            {
                case PacketType.Register:
                    return HandleRegistration(request);
                case PacketType.RegisterResponse:
                    LogMessage("Received RegisterResponse packet type");
                    Logger.Write("REGISTER PACKET", "Received RegisterResponse packet type");
                    return HandleRegistration(request);

                case PacketType.Login:
                    return HandleLogin(request, tcpClient);
                case PacketType.LoginResponse:
                    LogMessage("Received LoginResponse packet type");
                    Logger.Write("LOGIN PACKET", "Received LoginResponse packet type");
                    return HandleLogin(request, tcpClient);

                default:
                    LogMessage($"Unknown packet type: {request.Type}");
                    Logger.Write("UNKNOWN PACKET", $"Unknown packet type: {request.Type}");
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

        private Packet? LoginValidation(Packet request, string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                LogMessage("Login failed: Missing username or password");
                Logger.Write("LOGIN", "Login failed: Missing username or password");

                return new Packet
                {
                    Type = PacketType.LoginResponse,
                    Success = false,
                    Message = "Username and password are required",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", "Username and password are required" }
                    }
                };
            }

            return null; 
        }


        private Packet HandleLogin(Packet request, TcpClient tcpClient)
        {
            var connection = new MySqlConnection(connectionString);

            try
            {
                connection.Open();

                LogMessage($"Processing login for username: {request.Data["username"]}");
                Logger.Write("LOGIN", $"Processing login for username: {request.Data["username"]}");

                string username = request.Data["username"];
                string password = request.Data["password"];

                Packet? validationResult = LoginValidation(request, username, password);

                if (IsAccountLocked(username))
                {
                    Logger.Write("ACCOUNT", $"Account is locked for {username}.");

                    return new Packet
                    {
                        Type = PacketType.LoginResponse,
                        Success = false,
                        Message = "Account is locked. Please try again later or contact admin.",
                        Data = new Dictionary<string, string>
                        {
                            { "success", "false" },
                            { "message", "Account is locked. Please try again later or contact admin." }
                        }
                    };
                }

                string query = @"SELECT id, username, role FROM users 
                    WHERE username = @username AND password = @password";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@username", username);
                    command.Parameters.AddWithValue("@password", HashPassword(password));

                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            int user_id = GetUserId(username);
                            string ipAddress = GetClientIpAddress(tcpClient);

                            IncrementFailedLoginAttempts(username);

                            RecordLoginAttempt(user_id, false, ipAddress);

                            int failedAttempts = GetFailedLoginAttempts(username);

                            if (failedAttempts >= 5)
                            {
                                // Lock the account
                                LockAccount(username);

                                LogMessage($"Account locked: Too many failed attempts for username: {username}");
                                Logger.Write("LOGIN", $"Account locked: Too many failed attempts for username: {username}");
                                Logger.Write("LOGIN", $"Failed attempts for {username}: {failedAttempts}");

                                return new Packet
                                {
                                    Type = PacketType.LoginResponse,
                                    Success = false,
                                    Message = "Account temporarily locked. Please try again later or contact admin.",
                                    Data = new Dictionary<string, string>
                                    {
                                        { "success", "false" },
                                        { "message", "Account temporarily locked. Please try again later or contact admin." }
                                    }
                                };
                            }

                            LogMessage($"Login failed: Invalid credentials for username: {username}");
                            Logger.Write("LOGIN", $"Failed login attempt for {username} from IP: {ipAddress}");

                            return new Packet
                            {
                                Type = PacketType.LoginResponse,
                                Success = false,
                                Message = "Invalid username or password",
                                Data = new Dictionary<string, string>
                                {
                                    { "success", "false" },
                                    { "message", "Invalid username or password" }
                                }
                            };
                        }

                        int userId = reader.GetInt32("id");
                        string userRole = reader.GetString("role");

                        ResetFailedAttempts(username);
                        ResetAccountLocked(username);

                        RecordLoginAttempt(userId, true, GetClientIpAddress(tcpClient));
                        Logger.Write("LOGIN", $"Successful login for {username} with role: {userRole}");

                        return new Packet
                        {
                            Type = PacketType.LoginResponse,
                            Success = true,
                            Message = "Login successful",
                            Data = new Dictionary<string, string>
                                {
                                    { "success", "true" },
                                    { "message", "Login successful" },
                                    { "userId", userId.ToString() },
                                    { "username", username },
                                    { "role", userRole }
                                }
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Login error: {ex.Message}");
                Logger.Write("LOGIN", $"Login error: {ex.Message}");

                return new Packet
                {
                    Type = PacketType.LoginResponse,
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

        private Packet HandleRegistration(Packet request)
        {
            try
            {
                LogMessage($"Processing registration for username: {request.Data["username"]}");
                Logger.Write("REGISTRATION", $"Processing registration for username: {request.Data["username"]}");

                if (!request.Data.ContainsKey("username") || !request.Data.ContainsKey("password"))
                {
                    LogMessage("Registration failed: Missing username or password");
                    Logger.Write("REGISTRATION", "Registration failed: Missing username or password");

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
                    Logger.Write("REGISTRATION", "Database connection opened");

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
                    Logger.Write("REGISTRATION", "$\"Successfully registered new user: {username}\"");

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
                Logger.Write("REGISTRATION", $"Registration error: {ex.Message}");

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

        private string GetClientIpAddress(TcpClient tcpClient)
        {
            try
            {
                var endpoint = tcpClient?.Client?.RemoteEndPoint as IPEndPoint;
                return endpoint?.Address.ToString() ?? "Unknown";
            }
            catch (Exception ex)
            {
                Logger.Write("ERROR", $"Failed to get client IP: {ex.Message}");
                return "Unknown";
            }
        }

        private int GetUserId(string username)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT id FROM users WHERE username = @username";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        var result = command.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : -1;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write("ERROR", $"Failed to get user ID: {ex.Message}");
                return -1;
            }
        }

        private bool IsAccountLocked(string username)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    var query = @"
                        SELECT locked_time, unlock_time
                        FROM account_locks 
                        WHERE user_id = @userId
                        AND unlock_time > NOW()";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", GetUserId(username));

                        using (var reader = command.ExecuteReader())
                        {
                            return reader.HasRows;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write("ERROR", $"Error checking account lock status: {ex.Message}");
                return false;
            }
        }

        private void IncrementFailedLoginAttempts(string username)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"UPDATE users 
                           SET failed_attempts = failed_attempts + 1 
                           WHERE username = @username";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write("ERROR", $"Failed to increment login attempts: {ex.Message}");
            }
        }

        private void ResetAccountLocked(string username)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"UPDATE users
                           SET account_locked = 0, lockout_time = NULL
                           WHERE username = @username";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        command.ExecuteNonQuery();
                    }

                    string deleteQuery = @"DELETE FROM account_locks
                                 WHERE user_id = @userId";

                    using (var deleteCommand = new MySqlCommand(deleteQuery, connection))
                    {
                        deleteCommand.Parameters.AddWithValue("@userId", GetUserId(username));
                        deleteCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write("ERROR", $"Failed to reset login attempts: {ex.Message}");
            }
        }

        private void ResetFailedAttempts(string username)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"UPDATE users 
                           SET failed_attempts = 0 
                           WHERE username = @username";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write("ERROR", $"Failed to reset login attempts: {ex.Message}");
            }
        }

        private void RecordLoginAttempt(int userId, bool isSuccessful, string ipAddress)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"INSERT INTO login_attempts 
                           (user_id, attempt_time, is_successful, ip_address) 
                           VALUES (@userId, @attemptTime, @isSuccessful, @ipAddress)";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@userId", userId);
                        command.Parameters.AddWithValue("@attemptTime", DateTime.Now);
                        command.Parameters.AddWithValue("@isSuccessful", isSuccessful);
                        command.Parameters.AddWithValue("@ipAddress", ipAddress);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write("ERROR", $"Failed to record login attempt: {ex.Message}");
            }
        }

        private void LockAccount(string username)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    string lockQuery = @"INSERT INTO account_locks 
                               (user_id, locked_time, unlock_time, reason)
                               SELECT id, NOW(), DATE_ADD(NOW(), INTERVAL 10 MINUTE), @reason
                               FROM users WHERE username = @username";

                    using (var lockCommand = new MySqlCommand(lockQuery, connection))
                    {
                        lockCommand.Parameters.AddWithValue("@username", username);
                        lockCommand.Parameters.AddWithValue("@reason", "Too many failed login attempts");
                        lockCommand.ExecuteNonQuery();
                    }

                    string updateQuery = @"UPDATE users 
                                 SET account_locked = 1, 
                                     lockout_time = DATE_ADD(NOW(), INTERVAL 10 MINUTE) 
                                 WHERE username = @username";

                    using (var updateCommand = new MySqlCommand(updateQuery, connection))
                    {
                        updateCommand.Parameters.AddWithValue("@username", username);
                        updateCommand.ExecuteNonQuery();
                    }

                    Logger.Write("SECURITY", $"Account locked for user: {username} for 10 minutes");
                }
            }
            catch (Exception ex)
            {
                Logger.Write("ERROR", $"Failed to lock account: {ex.Message}");
            }
        }

        private int GetFailedLoginAttempts(string username)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"SELECT failed_attempts 
                           FROM users
                           WHERE username = @username";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", username);
                        return Convert.ToInt32(command.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write("ERROR", $"Failed to get failed login attempts: {ex.Message}");
                return 0;
            }
        }

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
