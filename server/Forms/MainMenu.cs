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
using System.Net.Http;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.VisualBasic.ApplicationServices;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using server.Controllers;

namespace server.Forms
{
    public partial class MainMenu : Form
    {
        private TcpListener? listener;
        private bool isServerRunning;
        private int serverPort = 8888;

        private Point dragOffset;
        private bool isDragging = false;

        private readonly string connectionString = DatabaseManager.Instance.ConnectionString;

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
            ConnectToDatabase();
        }

        private void ConnectToDatabase()
        {
            string message = string.Empty;

            try
            {
                btnConnectToDB.Enabled = false;
                message = "Connecting...";
                btnConnectToDB.Text = message;
                Task.Delay(50);

                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    LogMessage("Database connection successful");
                    btnConnectToDB.Enabled = false;
                    message = "Connect To Database";
                    btnConnectToDB.Text = message;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Database connection error: {ex.Message}");
                MessageBox.Show("Could not connect to database. Please check your connection settings.",
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnConnectToDB.Enabled = true;
                message = "Reconnect To Database";
                btnConnectToDB.Text = message;
            }
        }

        private void btnConnectToDB_Click(object sender, EventArgs e)
        {
            ConnectToDatabase();
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
                using (var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true))
                {
                    string jsonRequest = (await reader.ReadLineAsync())!;
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
                    Logger.Write("SERVER RESPONSE", $"Sent to {clientAddress}: {jsonResponse}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error handling client: {ex.Message}");
                Logger.Write("EXCEPTION", $"Error handling client: {ex.Message}");
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

            LogMessage($"Processing request type: {request.Type}");
            Logger.Write("CLIENT REQUEST", $"Processing request type: {request.Type}");

            switch (request.Type)
            {
                // Auth
                case PacketType.Login:
                    return authController.Login(request, tcpClient);
                case PacketType.LoginResponse:
                    LogMessage("Received LoginResponse packet type");
                    Logger.Write("LOGIN PACKET", "Received LoginResponse packet type");
                    return authController.Login(request, tcpClient);
                case PacketType.Register:
                    return authController.Register(request);
                case PacketType.RegisterResponse:
                    LogMessage("Received RegisterResponse packet type");
                    Logger.Write("REGISTER PACKET", "Received RegisterResponse packet type");
                    return authController.Register(request);

                // Product
                case PacketType.GetProduct:
                    return productController.Get(request);
                case PacketType.GetProductResponse:
                    LogMessage("Received get product packet type");
                    Logger.Write("GET PRODUCT PACKET", "Received get product packet type");
                    return productController.Get(request);
                case PacketType.CreateProduct:
                    return productController.Create(request);
                case PacketType.CreateProductResponse:
                    LogMessage("Received product creation packet type");
                    Logger.Write("CREATE PRODUCT PACKET", "Received product creation packet type");
                    return productController.Create(request);

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

        private void button1_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Clear logs?", "Clear", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                rtbLogs.Clear();
            }
        }

        private void btnCloseWindow_Click(object sender, EventArgs e)
        {
            if (StopServerWithConfirmation())
            {
                LogMessage("Server stopped by user request");
                System.Windows.Forms.Application.Exit();
            }
        }

        private void btnMinimizeWindow_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }
    }
}
