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
using Org.BouncyCastle.Tls;
using MySqlX.XDevAPI.Common;
using static System.ComponentModel.Design.ObjectSelectorEditor;
using Mysqlx.Session;
using server.Services;

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

        public Packet BackupDataAuth(Packet request)
        {
            // Input validation
            if (request?.Data == null ||
                !request.Data.TryGetValue("username", out string? username) ||
                !request.Data.TryGetValue("password", out string? password))
            {
                Logger.Write("BACKUP_AUTH", "Invalid request data");
                return CreateAuthResponse(false, "Invalid request data");
            }

            try
            {
                using (var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString))
                {
                    connection.Open();

                    Logger.Write("BACKUP_AUTH", $"Processing auth for user: {username}");

                    const string query = @"SELECT * FROM users 
                                WHERE username = @username 
                                AND password = @password 
                                LIMIT 1";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@username", username.Trim());
                        command.Parameters.AddWithValue("@password", HashPassword(password));

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                reader.Read();
                                Logger.Write("BACKUP_AUTH", $"Authentication successful for {username}");

                                return CreateAuthResponse(
                                    true,
                                    "Authentication successful",
                                    new Dictionary<string, string>
                                    {
                                        { "user_id", reader["id"].ToString() ?? "" },
                                        { "role", reader["role"].ToString() ?? "" }
                                    });
                            }

                            Logger.Write("BACKUP_AUTH", $"Authentication failed for {username}");
                            return CreateAuthResponse(false, "Invalid credentials");
                        }
                    }
                }
            }
            catch (MySqlException dbEx)
            {
                Logger.WriteError("BACKUP_AUTH_DB", "Database error during authentication", dbEx);
                return CreateAuthResponse(false, "Database error");
            }
            catch (Exception ex)
            {
                Logger.WriteError("BACKUP_AUTH", "Unexpected error during authentication", ex);
                return CreateAuthResponse(false, "Internal server error");
            }
        }

        private Packet CreateAuthResponse(bool success, string message, Dictionary<string, string>? additionalData = null)
        {
            var response = new Packet
            {
                Type = PacketType.BackupDataAuth,
                Success = success,
                Message = message,
                Data = new Dictionary<string, string>
                {
                    { "success", success.ToString().ToLower() },
                    { "message", message }
                }
            };

            if (additionalData != null)
            {
                foreach (var kvp in additionalData)
                {
                    response.Data.Add(kvp.Key, kvp.Value);
                }
            }

            return response;
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




        // SERVER AUTH
        public string? ServerLogin(string username, string password)
        {
            string sessionToken = string.Empty;
            string hashedPassword = HashPassword(password);

            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show("Username and password cannot be empty.", "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return null;
                }

                using (var connection = new MySqlConnection(ServerDatabaseManager.Instance.ServerConnectionString))
                {
                    connection.Open();

                    using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                    {
                        try
                        {
                            var cleanupCommand = new MySqlCommand(
                                "DELETE FROM user_sessions WHERE expires_at < NOW()",
                                connection, transaction
                            );
                            int rowsAffected = cleanupCommand.ExecuteNonQuery();

                            int userId;

                            using (var userCommand = new MySqlCommand(
                                "SELECT id FROM users WHERE username = @Username AND password = @Password",
                                connection, transaction))
                            {
                                userCommand.Parameters.AddWithValue("@Username", username);
                                userCommand.Parameters.AddWithValue("@Password", password);

                                var result = userCommand.ExecuteScalar();
                                if (result == null || result == DBNull.Value)
                                {
                                    MessageBox.Show("Invalid credentials.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return null;
                                }
                                userId = Convert.ToInt32(result);
                            }

                            string sessionTokenFromDb = string.Empty;
                            int sessionTimeDiff = -1;

                            using (var checkSessionCommand = new MySqlCommand(
                                "SELECT session_token, TIMESTAMPDIFF(SECOND, last_activity, NOW()) AS time_diff " +
                                "FROM user_sessions WHERE user_id = @UserId AND is_active = TRUE AND expires_at > NOW() FOR UPDATE",
                                connection, transaction))
                            {
                                checkSessionCommand.Parameters.AddWithValue("@UserId", userId);
                                using (var reader = checkSessionCommand.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        sessionTokenFromDb = reader.GetString("session_token");
                                        sessionTimeDiff = reader.GetInt32("time_diff");
                                    }
                                }
                            }

                            if (sessionTimeDiff == -1)
                            {
                                // No active session
                            }
                            else if (sessionTimeDiff > 180) // Session expired 3 minutes
                            {
                                string deleteSessionQuery = "DELETE FROM user_sessions WHERE user_id = @UserId AND session_token = @SessionToken";
                                using (var deleteCommand = new MySqlCommand(deleteSessionQuery, connection, transaction))
                                {
                                    deleteCommand.Parameters.AddWithValue("@UserId", userId);
                                    deleteCommand.Parameters.AddWithValue("@SessionToken", sessionTokenFromDb);
                                    deleteCommand.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                return "ALREADY_LOGGED_IN";
                            }

                            var invalidateCommand = new MySqlCommand(
                                "UPDATE user_sessions SET is_active = FALSE WHERE user_id = @UserId AND is_active = TRUE",
                                connection, transaction
                            );
                            invalidateCommand.Parameters.AddWithValue("@UserId", userId);
                            invalidateCommand.ExecuteNonQuery();

                            sessionToken = Guid.NewGuid().ToString();
                            using (var sessionCommand = new MySqlCommand(
                                @"INSERT INTO user_sessions 
                                (user_id, session_token, created_at, expires_at, last_activity, is_active) 
                                VALUES (@userId, @sessionToken, NOW(), DATE_ADD(NOW(), INTERVAL 24 HOUR), NOW(), TRUE)",
                                connection, transaction))
                            {
                                sessionCommand.Parameters.AddWithValue("@userId", userId);
                                sessionCommand.Parameters.AddWithValue("@sessionToken", sessionToken);
                                sessionCommand.ExecuteNonQuery();
                            }

                            transaction.Commit();

                            return sessionToken;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            MessageBox.Show($"Error occurred: {ex.Message}", "Transaction Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to login: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        public void ServerRegister(string username, string password, string confirmpass)
        {
            using (var connection = new MySqlConnection(ServerDatabaseManager.Instance.ServerConnectionString))
            {
                connection.Open();
            }
        }

        public bool ServerLogout(string sessionToken)
        {
            if (string.IsNullOrWhiteSpace(sessionToken))
            {
                Logger.Write("LOGOUT_ERROR", "Empty session token provided");
                return false;
            }

            try
            {
                using (var connection = new MySqlConnection(ServerDatabaseManager.Instance.ServerConnectionString))
                {
                    connection.Open();

                    // Use the `is_active` field to invalidate the session explicitly
                    string logoutQuery = @"UPDATE user_sessions 
                                   SET is_active = FALSE, expires_at = NOW() 
                                   WHERE session_token = @sessionToken
                                   AND is_active = TRUE";

                    using (var command = new MySqlCommand(logoutQuery, connection))
                    {
                        command.Parameters.AddWithValue("@sessionToken", sessionToken);
                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            SessionManager.Instance.Logout();
                            Logger.Write("LOGOUT_SUCCESS", $"Session terminated: {sessionToken[..8]}...");
                            return true;
                        }
                        else
                        {
                            Logger.Write("LOGOUT_WARNING", $"Session not found or already expired: {sessionToken[..8]}...");
                            return false;
                        }
                    }
                }
            }
            catch (MySqlException ex) when (ex.Number == 1292)
            {
                Logger.Write("LOGOUT_DATETIME_ERROR", $"Invalid datetime: {ex.Message}");
                throw new InvalidOperationException("Logout failed due to system error", ex);
            }
            catch (Exception ex)
            {
                Logger.Write("LOGOUT_CRITICAL", $"Unexpected error: {ex}");
                throw;
            }
        }

        public bool ValidateSession(string token)
        {
            using (var connection = new MySqlConnection(ServerDatabaseManager.Instance.ServerConnectionString))
            {
                connection.Open();

                string sql = @"UPDATE user_sessions 
                      SET last_activity = NOW() 
                      WHERE session_token = @token 
                      AND expires_at > NOW()";

                using (var cmd = new MySqlCommand(sql, connection))
                {
                    cmd.Parameters.AddWithValue("@token", token);
                    return cmd.ExecuteNonQuery() > 0;
                }
            }
        }

        public void CleanupExpiredSessions()
        {
            try
            {
                using (var connection = new MySqlConnection(ServerDatabaseManager.Instance.ServerConnectionString))
                {
                    connection.Open();

                    string cleanupQuery = @"DELETE FROM user_sessions 
                                  WHERE expires_at < DATE_SUB(NOW(), INTERVAL 7 DAY) 
                                  OR (expired_at IS NOT NULL AND expired_at < DATE_SUB(NOW(), INTERVAL 7 DAY))";

                    using (var command = new MySqlCommand(cleanupQuery, connection))
                    {
                        int cleaned = command.ExecuteNonQuery();
                        Logger.Write("SESSION_CLEANUP", $"Removed {cleaned} expired sessions");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write("CLEANUP_ERROR", $"Failed to clean sessions: {ex.Message}");
            }
        }

        public void RedirectTo(Form newForm)
        {
            foreach (Form openForm in System.Windows.Forms.Application.OpenForms)
            {
                if (openForm != newForm)
                {
                    openForm.Hide();
                }
            }

            newForm.StartPosition = FormStartPosition.CenterScreen;
            newForm.Show();

            if (System.Windows.Forms.Application.OpenForms.Count == 0)
            {
                newForm.FormClosed += (s, args) => System.Windows.Forms.Application.Exit();
            }
        }

        public void UpdateSessionHeartbeat(string sessionToken)
        {
            try
            {
                using (var connection = new MySqlConnection(ServerDatabaseManager.Instance.ServerConnectionString))
                {
                    connection.Open();

                    using (var command = new MySqlCommand(
                        "UPDATE user_sessions SET last_activity = NOW() WHERE session_token = @sessionToken AND expires_at > NOW() AND is_active = TRUE",
                        connection))
                    {
                        command.Parameters.AddWithValue("@sessionToken", sessionToken);
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write("SESSION_UPDATE_ERROR", ex.Message);
            }
        }


        public void CleanupInactiveSessions()
        {
            try
            {
                using (var connection = new MySqlConnection(ServerDatabaseManager.Instance.ServerConnectionString))
                {
                    connection.Open();

                    // Invalidate sessions that have not had a heartbeat within the last 3 minutes
                    using (var command = new MySqlCommand(
                        "UPDATE user_sessions SET is_active = FALSE WHERE last_activity < DATE_SUB(NOW(), INTERVAL 3 MINUTE) AND is_active = TRUE",
                        connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write("SESSION_CLEANUP_ERROR", ex.Message);
            }
        }
    }
}