using MySql.Data.MySqlClient;
using server.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.Services
{
    public class SessionManager
    {
        private static SessionManager? _instance;
        private string? _currentSessionToken;

        public static SessionManager Instance => _instance ??= new SessionManager();

        public string? CurrentSessionToken
        {
            get => _currentSessionToken;
            set => _currentSessionToken = value;
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
        }
    }
}
