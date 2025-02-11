using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace server.Database
{
    public class DatabaseManager
    {
        private readonly string connectionString;

        public DatabaseManager(string host, string database, string username, string password)
        {
            connectionString = $"Server={host};Database={database};Uid={username};Pwd={password};";
        }

        public bool ValidateLogin(string username, string password)
        {
            using var connection = new MySqlConnection(connectionString);
            try
            {
                connection.Open();
                string query = "SELECT COUNT(*) FROM users WHERE username = @username AND password = @password";
                using var command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@password", password);

                int count = Convert.ToInt32(command.ExecuteScalar());
                return count > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Database error: {ex.Message}");
                return false;
            }
        }

        public bool RegisterUser(string username, string password)
        {
            using var connection = new MySqlConnection(connectionString);
            try
            {
                connection.Open();

                // Check if username exists
                string checkQuery = "SELECT COUNT(*) FROM users WHERE username = @username";
                using var checkCommand = new MySqlCommand(checkQuery, connection);
                checkCommand.Parameters.AddWithValue("@username", username);
                int count = Convert.ToInt32(checkCommand.ExecuteScalar());

                if (count > 0)
                    return false;

                string insertQuery = "INSERT INTO users (username, password) VALUES (@username, @password)";
                using var insertCommand = new MySqlCommand(insertQuery, connection);
                insertCommand.Parameters.AddWithValue("@username", username);
                insertCommand.Parameters.AddWithValue("@password", password);

                return insertCommand.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Database error: {ex.Message}");
                return false;
            }
        }
    }
}
