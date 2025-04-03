using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.Database
{
    internal class ServerDatabaseManager
    {
        private static ServerDatabaseManager? _instance;
        private readonly string serverConnectionString;

        public static ServerDatabaseManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ServerDatabaseManager("localhost", "dbserver", "root", "");
                }
                return _instance;
            }
        }

        public string ServerConnectionString => serverConnectionString;

        private ServerDatabaseManager(string host, string database, string username, string password)
        {
            serverConnectionString = $"Server={host};Database={database};Uid={username};Pwd={password};";
        }
    }
}
