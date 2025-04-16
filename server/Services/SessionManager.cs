using client.Helpers;
using MySql.Data.MySqlClient;
using server.Database;
using server.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace server.Services
{
    public class SessionManager
    {
        private static SessionManager? _instance;
        private string? _currentSessionToken;
        private System.Timers.Timer? _heartbeatTimer;
        private string? _currentUsername;

        public static event Action? ForceLogoutUser;

        public static SessionManager Instance => _instance ??= new SessionManager();

        public string? CurrentSessionToken
        {
            get => _currentSessionToken;
            set
            {
                _currentSessionToken = value;
                if (!string.IsNullOrEmpty(value))
                {
                    StartHeartbeat();
                }
                else
                {
                    StopHeartbeat();
                }
            }
        }

        public string? CurrentUsername
        {
            get => _currentUsername;
            set
            {
                _currentUsername = value;
            }
        }

        public bool IsSessionValid()
        {
            if (string.IsNullOrEmpty(_currentSessionToken))
                return false;

            return ValidateSession(_currentSessionToken);
        }

        private bool ValidateSession(string sessionToken)
        {
            using (var connection = new MySqlConnection(ServerDatabaseManager.Instance.ServerConnectionString))
            {
                connection.Open();
                string query = @"SELECT COUNT(*) FROM user_sessions 
                                 WHERE session_token = @sessionToken 
                                 AND expires_at > NOW()";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@sessionToken", sessionToken);
                    return Convert.ToInt32(command.ExecuteScalar()) > 0;
                }
            }
        }

        public void Logout()
        {
            if (!string.IsNullOrEmpty(_currentSessionToken))
            {
                using (var connection = new MySqlConnection(ServerDatabaseManager.Instance.ServerConnectionString))
                {
                    connection.Open();
                    string query = "DELETE FROM user_sessions WHERE session_token = @sessionToken";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@sessionToken", _currentSessionToken);
                        command.ExecuteNonQuery();
                    }
                }
            }
            _currentSessionToken = null;
            StopHeartbeat();
        }

        // Start the heartbeat that updates the last_activity field every 3 minutes
        private void StartHeartbeat()
        {
            if (_heartbeatTimer != null)
            {
                _heartbeatTimer.Stop();
                _heartbeatTimer.Dispose();
            }

            _heartbeatTimer = new System.Timers.Timer(180000); // 3 minutes in milliseconds
            _heartbeatTimer.Elapsed += OnHeartbeatElapsed;
            _heartbeatTimer.Start();
        }

        // Method that runs every 3 minutes to update the last activity in the database
        private void OnHeartbeatElapsed(object? sender, ElapsedEventArgs e)
        {
            UpdateSessionHeartbeat(_currentSessionToken!);
        }

        // Update the session's last_activity field in the database
        private void UpdateSessionHeartbeat(string sessionToken)
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
                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            //Logger.Write("SESSION_HEARTBEAT", $"\u2764 Received heartbeat for session: {sessionToken}");
                        }
                        else
                        {
                            Logger.Write("SESSION_HEARTBEAT", $"No active session found for session token: {sessionToken}");
                            StopHeartbeat();

                            // Lock screen instead of force logout.
                            if (ForceLogoutUser != null && ForceLogoutUser.GetInvocationList().Length > 0)
                            {
                                foreach (var handler in ForceLogoutUser.GetInvocationList())
                                {
                                    try
                                    {
                                        if (handler.Target is Control control && control.InvokeRequired)
                                        {
                                            control.Invoke(handler);
                                        }
                                        else
                                        {
                                            handler.DynamicInvoke();
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Write("SESSION_ERROR", $"Handler invocation failed: {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write("SESSION_HEARTBEAT", $"Error updating session heartbeat: {ex.Message}");
            }
        }

        // Stop the heartbeat timer when the session is invalidated or logged out
        private void StopHeartbeat()
        {
            if (_heartbeatTimer != null)
            {
                _heartbeatTimer.Stop();
                _heartbeatTimer.Dispose();
                _heartbeatTimer = null;
            }
        }
    }
}
