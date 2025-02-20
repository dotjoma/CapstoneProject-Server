using client.Helpers;
using MySql.Data.MySqlClient;
using server.Core.Network;
using server.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Diagnostics;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Cmp;
using System.Security.Cryptography;
using System.Collections;
using System.Net.Http;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.VisualBasic.ApplicationServices;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using server.Controllers;

namespace server.Controllers
{
    public class AuthController
    {

        public Packet Register(Packet request)
        {
            try
            {
                Logger.Write("REGISTRATION", $"Processing registration for username: {request.Data["username"]}");

                if (!request.Data.ContainsKey("username") || !request.Data.ContainsKey("password"))
                {
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

                using (var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString))
                {
                    connection.Open();
                    Logger.Write("REGISTRATION", "Database connection opened");

                    // Check if username exists
                    string checkQuery = "SELECT COUNT(*) FROM users WHERE username = @username";
                    using (var checkCommand = new MySqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@username", username);
                        int userCount = Convert.ToInt32(checkCommand.ExecuteScalar());

                        if (userCount > 0)
                        {
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

        public Packet Login(Packet request, TcpClient tcpClient)
        {
            var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString);

            try
            {
                connection.Open();

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

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
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
                using (var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString))
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
                using (var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString))
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
                using (var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString))
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
                using (var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString))
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
                using (var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString))
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
                using (var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString))
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
                using (var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString))
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

        private Packet? LoginValidation(Packet request, string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
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

        private int GetFailedLoginAttempts(string username)
        {
            try
            {
               using (var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString))
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
    }
}
