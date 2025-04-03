using MySql.Data.MySqlClient;
using server.Database;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace server.Services
{
    public class ServerStatus
    {
        private static readonly Lazy<ServerStatus> _instance =
            new Lazy<ServerStatus>(() => new ServerStatus());

        private string _status = "Stopped";
        private bool _maintenanceMode = false;
        private readonly Stopwatch _uptimeTimer = new Stopwatch();
        private int _activeConnections = 0;
        private readonly object _statusLock = new object();

        public DateTime StartTime { get; private set; }
        public TimeSpan Uptime => _uptimeTimer.Elapsed;
        public int ActiveConnections => _activeConnections;
        public bool IsMaintenanceMode => _maintenanceMode;
        public int RequestsProcessed { get; private set; }
        public float CpuUsage { get; private set; }
        public long MemoryUsage { get; private set; }

        public static ServerStatus Instance => _instance.Value;

        public string CurrentStatus
        {
            get
            {
                lock (_statusLock)
                {
                    return _status;
                }
            }
            private set
            {
                lock (_statusLock)
                {
                    _status = value;
                }
            }
        }

        private ServerStatus() { }

        public void StartServer()
        {
            lock (_statusLock)
            {
                if (CurrentStatus == "Running") return;

                StartTime = DateTime.UtcNow;
                _uptimeTimer.Start();
                CurrentStatus = "Running";
                _maintenanceMode = false;
            }
        }

        public void StopServer()
        {
            lock (_statusLock)
            {
                _uptimeTimer.Reset();
                CurrentStatus = "Stopped";
                _activeConnections = 0;
            }
        }

        public void EnterMaintenanceMode(string reason)
        {
            lock (_statusLock)
            {
                _maintenanceMode = true;
                CurrentStatus = $"Maintenance: {reason}";
            }
        }

        public void ExitMaintenanceMode()
        {
            lock (_statusLock)
            {
                _maintenanceMode = false;
                CurrentStatus = "Running";
            }
        }

        public void IncrementConnections()
        {
            Interlocked.Increment(ref _activeConnections);
        }

        public void DecrementConnections()
        {
            Interlocked.Decrement(ref _activeConnections);
        }

        public void UpdateSystemMetrics()
        {
            using (var process = Process.GetCurrentProcess())
            {
                CpuUsage = process.TotalProcessorTime.Ticks;
                MemoryUsage = process.WorkingSet64 / 1024 / 1024;
            }
        }

        public Dictionary<string, object> GetStatusReport()
        {
            lock (_statusLock)
            {
                return new Dictionary<string, object>
                {
                    ["status"] = CurrentStatus,
                    ["uptime"] = Uptime.ToString(@"hh\:mm\:ss"),
                    ["connections"] = ActiveConnections,
                    ["maintenance"] = IsMaintenanceMode,
                    ["requests"] = RequestsProcessed,
                    ["cpu"] = CpuUsage,
                    ["memory"] = $"{MemoryUsage} MB",
                    ["startTime"] = StartTime.ToString("yyyy-MM-dd HH:mm:ss")
                };
            }
        }

        public void SaveToDatabase()
        {
            const string query = @"
            INSERT INTO server_status 
            (status, uptime, connections, maintenance_mode, cpu_usage, memory_usage, requests_processed, timestamp)
            VALUES 
            (@status, @uptime, @connections, @maintenance, @cpuUsage, @memoryUsage, @requests, NOW())";

            using (var connection = new MySqlConnection(ServerDatabaseManager.Instance.ServerConnectionString))
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@status", CurrentStatus);
                cmd.Parameters.AddWithValue("@uptime", (int)Uptime.TotalSeconds);
                cmd.Parameters.AddWithValue("@connections", ActiveConnections);
                cmd.Parameters.AddWithValue("@maintenance", IsMaintenanceMode);
                cmd.Parameters.AddWithValue("@cpuUsage", CpuUsage);
                cmd.Parameters.AddWithValue("@memoryUsage", MemoryUsage);
                cmd.Parameters.AddWithValue("@requests", RequestsProcessed);

                connection.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
