using server.Core.Network;
using System;
using Serilog;
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
using System.Net.Http;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.VisualBasic.ApplicationServices;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using server.Controllers;
using server.Services;
using System.Text.Json.Nodes;

namespace server.Forms
{
    public partial class MainMenu : Form
    {
        private TcpListener? listener;
        private bool isServerRunning;
        private int serverPort = 8888;

        private bool isConnectedToDatabase;

        private Point dragOffset;
        private bool isDragging = false;

        private readonly string connectionString = DatabaseManager.Instance.ConnectionString;

        private SystemLogs? _systemLogsInstance;
        private Dashboard? _dashboardInstace;

        public static event Action? OnServerStarted;
        public static event Action? OnServerStopped;
        public static event Action? OnLogOut;

        public MainMenu()
        {
            InitializeComponent();
            SetupForm();

            LoginForm.StartServerOnLogin += StartServerOnLogin;
            SessionManager.ForceLogoutUser += ForceLogoutRequest;
        }

        private void SetupForm()
        {
            UpdateStatus("Not Running");
        }

        private async void StartServerOnLogin()
        {
            await StartServer();
        }

        private void ForceLogoutRequest()
        {
            Logout();
        }

        private async void btnStartServer_Click(object? sender, EventArgs e)
        {
            await StartServer();
        }

        private async Task StartServer()
        {
            if (isServerRunning)
            {
                MessageBox.Show("Server is already running", "Server Status",
                               MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                if (listener != null && listener.Server.IsBound)
                {
                    Logger.Write("SERVER", "Attempted to start already bound listener");
                    return;
                }

                listener = new TcpListener(IPAddress.Any, serverPort);
                listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                listener.Start();
                isServerRunning = true;

                OnServerStarted?.Invoke();

                Logger.Write("SERVER STARTED", $"Server started on port {serverPort}");
                UpdateStatus("Running");

                await ListenForClients();
            }
            catch (SocketException sex) when (sex.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                Logger.WriteError("SERVER ERROR", $"Port {serverPort} is already in use", sex);
                MessageBox.Show($"Port {serverPort} is already in use", "Server Error",
                              MessageBoxButtons.OK, MessageBoxIcon.Error);
                StopServer();
            }
            catch (Exception ex)
            {
                Logger.WriteError("SERVER ERROR", $"Error starting server: {ex.Message}", ex);
                StopServer();
            }
        }

        private void btnStopServer_Click(object? sender, EventArgs e)
        {
            ForceStopServer();
        }

        private void MainMenu_Load(object sender, EventArgs e)
        {
            AddFormToPanel(new SystemLogs());
            Task.Delay(100);
            AddFormToPanel(new Dashboard());
            SetActiveForm(btnDashboard);
            Logger.Write("APPLICATION STARTED", "Server application started");
            ConnectToDatabase();
        }

        private void ConnectToDatabase()
        {
            string message = string.Empty;

            if (isConnectedToDatabase)
                return;

            try
            {
                btnConnectToDB.Enabled = false;
                message = "Connecting...";
                btnConnectToDB.Text = message;
                Task.Delay(50);

                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    Logger.Write("DATABASE CONNECTION", "Database connection successful");
                    btnConnectToDB.Enabled = false;
                    message = "Connect To Database";
                    btnConnectToDB.Text = message;
                    isConnectedToDatabase = true;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError("DATABASE ERROR", $"Database connection error: {ex.Message}", ex);
                MessageBox.Show("Could not connect to database. Please check your connection settings.",
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnConnectToDB.Enabled = true;
                message = "Reconnect To Database";
                btnConnectToDB.Text = message;
                isConnectedToDatabase = false;
            }
        }

        private void btnConnectToDB_Click(object sender, EventArgs e)
        {
            ConnectToDatabase();
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
                    Logger.Write("SERVER STOPPED", "Server stopped");
                    UpdateStatus("Stopped");
                }
                catch (Exception ex)
                {
                    Logger.WriteError("SERVER ERROR", $"Error stopping server during shutdown: {ex.Message}", ex);
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
                if (!isServerRunning)
                {
                    Logger.Write("SERVER", "Stop requested but server was not running");
                    MessageBox.Show("Server is not running.", "Information",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                isServerRunning = false;
                listener?.Stop();
                listener = null;
                OnServerStopped?.Invoke();

                Logger.Write("SERVER STOPPED", "Server stopped");
                UpdateStatus("Stopped");
            }
            catch (SocketException sex)
            {
                Logger.WriteError("SERVER STOP ERROR", $"Socket error while stopping: {sex.SocketErrorCode}", sex);
                listener = null;
                isServerRunning = false;
            }
            catch (Exception ex)
            {
                Logger.WriteError("SERVER ERROR", $"Unexpected error stopping server: {ex.Message}", ex);
            }
            finally
            {
                if (listener != null)
                {
                    listener.Server.Close();
                    listener = null;
                }
                isServerRunning = false;
            }
        }


        private async Task ListenForClients()
        {
            if (listener == null)
            {
                Logger.Write("LISTENER ERROR", "TCP listener is not initialized");
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
                        Logger.WriteError("CLIENT ACCEPT ERROR", $"Error accepting client: {ex.Message}", ex);
                    }
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            string clientAddress = "Unknown";

            try
            {
                var endpoint = client.Client.RemoteEndPoint as IPEndPoint;
                clientAddress = endpoint?.Address.ToString() ?? "Unknown";

                // Log client connection
                Logger.Write("CLIENT CONNECTED", $"Client connected from: {clientAddress}");

                using (client)
                using (NetworkStream stream = client.GetStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true))
                {
                    // Read the request from the client
                    string jsonRequest = (await reader.ReadLineAsync().ConfigureAwait(false))!;
                    var jsonNode = JsonNode.Parse(jsonRequest);
                    if (jsonNode?["Data"]?["image"] != null)
                    {
                        jsonNode["Data"]!["image"] = "[REDACTED]";
                    }

                    string sanitizedJson = jsonNode!.ToJsonString();

                    // Log the sanitized JSON
                    Logger.Write("CLIENT REQUEST", $"Received from {clientAddress}: {sanitizedJson}");

                    // Deserialize the request
                    Packet? request;
                    try
                    {
                        request = JsonConvert.DeserializeObject<Packet>(jsonRequest);
                        if (request == null)
                        {
                            Logger.Write("INVALID PACKET", $"Invalid packet received from {clientAddress}");
                            return;
                        }
                    }
                    catch (JsonException ex)
                    {
                        Logger.WriteError("DESERIALIZATION ERROR", $"Failed to deserialize packet from {clientAddress}", ex);
                        return;
                    }

                    // Process the request
                    var response = ProcessRequest(request, client);

                    // Serialize the response
                    string jsonResponse = JsonConvert.SerializeObject(response);
                    byte[] responseData = Encoding.UTF8.GetBytes(jsonResponse);

                    // Send the response to the client
                    await stream.WriteAsync(responseData, 0, responseData.Length).ConfigureAwait(false);

                    // Log the response (only Type, Success, and Message)
                    LogResponse(jsonResponse);
                }
            }
            catch (IOException ex)
            {
                Logger.WriteError("I/O ERROR", $"I/O error while handling client {clientAddress}", ex);
            }
            catch (SocketException ex)
            {
                Logger.WriteError("SOCKET ERROR", $"Socket error while handling client {clientAddress}", ex);
            }
            catch (Exception ex)
            {
                Logger.WriteError("UNEXPECTED ERROR", $"Unexpected error while handling client {clientAddress}", ex);
            }
            finally
            {
                Logger.Write("CLIENT DISCONNECTED", $"Client disconnected: {clientAddress}");
            }
        }

        private void LogResponse(string jsonResponse)
        {
            try
            {
                // Parse the JSON response
                var response = JsonConvert.DeserializeObject<dynamic>(jsonResponse);

                // Extract the required fields
                int type = response?.Type;
                bool success = response?.Success;
                string? message = response?.Message;

                // Log based on the type
                if (type == 12)
                {
                    Logger.Write("PRODUCTS RESPONSE", $"Type: {type}, Success: {success}, Message: {message}");
                }
                else
                {
                    Logger.Write("SERVER RESPONSE", $"Type: {type}, Success: {success}, Message: {message}");
                }
            }
            catch (Exception ex)
            {
                // Log errors if parsing fails
                Logger.WriteError("LOGGING ERROR", $"Failed to parse response: {ex.Message}", ex);
            }
        }

        private Packet ProcessRequest(Packet request, TcpClient tcpClient)
        {
            var authController = new AuthController();
            var productController = new ProductController();
            var unitController = new UnitController();
            var categoryController = new CategoryController();
            var subCategoryController = new SubCategoryController();
            var discountController = new DiscountController();
            var transactionController = new TransactionController();
            var salesReportController = new SalesReportController();
            var backupController = new BackupController();
            var auditTrailController = new AuditTrailController();

            Logger.Write("PROCESSING REQUEST", $"Processing request type: {request.Type}");

            switch (request.Type)
            {
                // Auth
                case PacketType.Login:
                    return authController.Login(request, tcpClient);
                case PacketType.LoginResponse:
                    return authController.Login(request, tcpClient);
                case PacketType.Register:
                    return authController.Register(request);
                case PacketType.RegisterResponse:
                    return authController.Register(request);

                // Product
                case PacketType.GetProduct:
                    return productController.Get(request);
                case PacketType.GetProductResponse:
                    return productController.Get(request);
                case PacketType.CreateProduct:
                    return productController.Create(request);
                case PacketType.CreateProductResponse:
                    return productController.Create(request);
                case PacketType.UpdateProduct:
                    return productController.Update(request);
                case PacketType.UpdateProductResponse:
                    return productController.Update(request);
                case PacketType.DeleteProduct:
                    return productController.Destroy(request);
                case PacketType.DeleteProductResponse:
                    return productController.Destroy(request);

                // Product Category
                case PacketType.GetCategory:
                    return categoryController.Get(request);
                case PacketType.GetCategoryResponse:
                    return categoryController.Get(request);
                case PacketType.CreateCategory:
                    return categoryController.Create(request);
                case PacketType.CreateCategoryResponse:
                    return categoryController.Create(request);

                // Product Subcategory
                case PacketType.GetSubCategory:
                    return subCategoryController.Get(request);
                case PacketType.GetSubCategoryResponse:
                    return subCategoryController.Get(request);
                case PacketType.CreateSubCategory:
                    return subCategoryController.Create(request);
                case PacketType.CreateSubCategoryResponse:
                    return subCategoryController.Create(request);
                case PacketType.GetAllSubcategory:
                    return subCategoryController.GetSubCategories(request);
                case PacketType.GetAllSubcategoryResponse:
                    return subCategoryController.GetSubCategories(request);

                // Product Unit
                case PacketType.GetUnit:
                    return unitController.Get(request);
                case PacketType.GetUnitResponse:
                    return unitController.Get(request);
                case PacketType.CreateUnit:
                    return unitController.Create(request);
                case PacketType.CreateUnitResponse:
                    return unitController.Create(request);

                // Discount
                case PacketType.CreateDiscount:
                    return discountController.Create(request);
                case PacketType.CreateDiscountResponse:
                    return discountController.Create(request);
                case PacketType.GetDiscount:
                    return discountController.Get(request);
                case PacketType.GetDiscountResponse:
                    return discountController.Get(request);

                // Transaction
                case PacketType.GenerateTransactionNumbers:
                    return transactionController.GenerateTransactionNumbers(request);
                case PacketType.GenerateTransactionNumbersResponse:
                    return transactionController.GenerateTransactionNumbers(request);
                case PacketType.SaveTransaction:
                    return transactionController.SaveTransaction(request);
                case PacketType.SaveTransactionResponse:
                    return transactionController.SaveTransaction(request);
                case PacketType.RemoveTransaction:
                    return transactionController.RemoveTransaction(request);
                case PacketType.RemoveTransactionResponse:
                    return transactionController.RemoveTransaction(request);
                case PacketType.ProcessTransaction:
                    return transactionController.ProcessTransaction(request);
                case PacketType.ProcessTransactionResponse:
                    return transactionController.ProcessTransaction(request);

                case PacketType.GetSalesReport:
                    return salesReportController.GetSalesReport(request);
                case PacketType.GetSalesReportResponse:
                    return salesReportController.GetSalesReport(request);

                // Backup & Restore
                case PacketType.GetBackupData:
                    return backupController.GetBackupData(request);
                case PacketType.GetBackupDataResponse:
                    return backupController.GetBackupData(request);
                case PacketType.RestoreData:
                    return backupController.Restore(request);
                case PacketType.RestoreDataResponse:
                    return backupController.Restore(request);

                case PacketType.BackupDataAuth:
                    return authController.BackupDataAuth(request);
                case PacketType.BackupDataAuthResponse:
                    return authController.BackupDataAuth(request);

                case PacketType.AuditSave:
                    return auditTrailController.SaveAudit(request);
                case PacketType.AuditSaveResponse:
                    return auditTrailController.SaveAudit(request);
                case PacketType.GetAudit:
                    return auditTrailController.GetAllAudit(request);
                case PacketType.GetAuditResponse:
                    return auditTrailController.GetAllAudit(request);

                default:
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

        private void UpdateStatus(string status)
        {
            if (lblStatus.InvokeRequired)
            {
                lblStatus.Invoke(new Action(() => UpdateStatus(status)));
                return;
            }

            lblStatus.Text = $"Server Status: {status}";
        }

        private void AddFormToPanel(Form form)
        {
            if (form == null) return;

            if (form is SystemLogs systemLogsForm)
            {
                _systemLogsInstance ??= systemLogsForm;

                if (!pnlContainer.Controls.Contains(_systemLogsInstance))
                {
                    InitializeFormInPanel(_systemLogsInstance);
                    pnlContainer.Controls.Add(_systemLogsInstance);
                }

                _systemLogsInstance.Show();
                _systemLogsInstance.BringToFront();
                return;
            }

            if (form is Dashboard dashboard)
            {
                _dashboardInstace ??= dashboard;

                if (!pnlContainer.Controls.Contains(_dashboardInstace))
                {
                    InitializeFormInPanel(_dashboardInstace);
                    pnlContainer.Controls.Add(_dashboardInstace);
                }

                _dashboardInstace.Show();
                _dashboardInstace.BringToFront();
                return;
            }

            foreach (Control control in pnlContainer.Controls.OfType<Form>().ToList())
            {
                if (control != form)
                {
                    control.Hide();
                }
            }

            if (!pnlContainer.Controls.Contains(form))
            {
                InitializeFormInPanel(form);
                pnlContainer.Controls.Add(form);
            }

            form.Show();
            form.BringToFront();
        }

        private void InitializeFormInPanel(Form form)
        {
            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Dock = DockStyle.Fill;
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

        private void button1_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Clear logs?", "Clear", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                ClearAllLogs();
            }
        }

        private void ClearAllLogs()
        {
            var logsForm = SystemLogs.Instance;

            if (!logsForm.IsHandleCreated)
            {
                var dummy = logsForm.Handle;
            }

            if (ReferenceEquals(logsForm, SystemLogs.Instance))
            {
                logsForm.Clear();
            }
            else
            {
                MessageBox.Show("Instance mismatch detected!");
            }
        }

        private void btnCloseWindow_Click(object sender, EventArgs e)
        {

        }

        private void btnMinimizeWindow_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void btnDashboard_Click(object sender, EventArgs e)
        {
            AddFormToPanel(new Dashboard());
            SetActiveForm(btnDashboard);
        }

        private void btnConnectedClients_Click(object sender, EventArgs e)
        {
            AddFormToPanel(new ConnectedClients());
            SetActiveForm(btnConnectedClients);
        }

        private void btnSystemLogs_Click(object sender, EventArgs e)
        {
            AddFormToPanel(new SystemLogs());
            SetActiveForm(btnSystemLogs);
        }

        private void SetActiveForm(Button activeButton)
        {
            foreach (Control control in buttonPanel.Controls)
            {
                if (control is Button button)
                {
                    button.BackColor = Color.Transparent;
                    button.ForeColor = Color.Black;
                }
            }

            activeButton.BackColor = Color.White;
            activeButton.ForeColor = Color.Black;
        }


        AuthController _authController = new AuthController();
        private void tsLogout_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to logout?", "Logout",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                OnLogOut?.Invoke();

                Logout();
            }
        }

        private async void Logout()
        {
            string? sessionToken = SessionManager.Instance.CurrentSessionToken ?? "";
            bool isSessionValid = _authController.ValidateSession(sessionToken);

            if (!isSessionValid)
            {
                OnLogOut?.Invoke();
                ForceStopServer();
                await Task.Delay(100);
                isServerRunning = false;
                listener?.Stop();
                listener = null;
                _authController.RedirectTo(new LoginForm());
            }

            isServerRunning = false;
            listener?.Stop();
            listener = null;

            bool success = _authController.ServerLogout(sessionToken);
            if (success)
            {
                await Task.Delay(100);
                _authController.RedirectTo(new LoginForm());
            }
        }

        private void fileToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void MainMenu_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isServerRunning)
            {
                var result = MessageBox.Show(
                    "Are you sure you want to stop the server and exit?",
                    "Stop Server",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }

                ForceStopServer();
                Logger.Write("SERVER STOPPED", "Server stopped by user request");
            }

            Logger.Write("APPLICATION CLOSING", $"Application closing: {e.CloseReason}");

            Environment.Exit(0);
        }
    }
}
