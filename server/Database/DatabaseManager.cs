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
        private static DatabaseManager? _instance;
        private readonly string connectionString;

        public static DatabaseManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DatabaseManager("localhost", "elicias", "root", "");
                }
                return _instance;
            }
        }

        public string ConnectionString => connectionString;

        private DatabaseManager(string host, string database, string username, string password)
        {
            connectionString = $"Server={host};Database={database};Uid={username};Pwd={password};";
        }
    }
}
