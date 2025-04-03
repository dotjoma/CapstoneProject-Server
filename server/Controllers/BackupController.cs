using client.Helpers;
using MySql.Data.MySqlClient;
using server.Core.Network;
using server.Database;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

namespace server.Controllers
{
    public class BackupController
    {
        public Packet GetBackupData(Packet packet)
        {
            using var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString);

            try
            {
                connection.Open();

                string tempBackupPath = Path.Combine(Path.GetTempPath(), $"backup_{Guid.NewGuid()}.sql");

                var cmd = new MySqlCommand("", connection);
                new MySqlBackup(cmd).ExportToFile(tempBackupPath);

                const int chunkSize = 64 * 1024;
                List<string> backupChunks = new();
                byte[] backupData = File.ReadAllBytes(tempBackupPath);

                for (int i = 0; i < backupData.Length; i += chunkSize)
                {
                    int size = Math.Min(chunkSize, backupData.Length - i);
                    backupChunks.Add(Convert.ToBase64String(backupData, i, size));
                }

                File.Delete(tempBackupPath);

                return new Packet
                {
                    Type = PacketType.GetBackupDataResponse,
                    Success = true,
                    Message = "Backup data success",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "true" },
                        { "message", "Backing up data success" },
                        { "backupChunks", JsonSerializer.Serialize(backupChunks) }, 
                        { "isEncrypted", "false" }
                    }
                };
            }
            catch (Exception ex)
            {
                return new Packet
                {
                    Type = PacketType.GetBackupDataResponse,
                    Success = false,
                    Message = $"Backup failed: {ex.Message}"
                };
            }
        }

        private static readonly ConcurrentDictionary<string, RestoreSession> _restoreSessions =
            new ConcurrentDictionary<string, RestoreSession>();

        public Packet Restore(Packet packet)
        {
            try
            {
                if (packet.Type == PacketType.RestoreData && packet.Data.ContainsKey("restoreId"))
                {
                    if (!packet.Data.ContainsKey("chunkIndex") || !packet.Data.ContainsKey("sqlChunk"))
                    {
                        _restoreSessions.TryRemove(packet.Data["restoreId"], out _);
                        return new Packet
                        {
                            Type = PacketType.RestoreDataResponse,
                            Success = true,
                            Message = "Cleaned up success",
                            Data = new Dictionary<string, string>
                            {
                                { "success", "true" },
                                { "message", "Cleaned up success" }
                            }
                        };
                    }
                }

                if (!packet.Data.ContainsKey("restoreId") || !packet.Data.ContainsKey("chunkIndex") ||
                    !packet.Data.ContainsKey("totalChunks") || !packet.Data.ContainsKey("sqlChunk"))
                {
                    return CreateErrorResponse("Missing required chunk data in restore request");
                }

                string restoreId = packet.Data["restoreId"];
                int chunkIndex = int.Parse(packet.Data["chunkIndex"]);
                int totalChunks = int.Parse(packet.Data["totalChunks"]);
                string chunkData = packet.Data["sqlChunk"];

                var session = _restoreSessions.GetOrAdd(restoreId, id => new RestoreSession
                {
                    Id = id,
                    TotalChunks = totalChunks,
                    Chunks = new ConcurrentDictionary<int, string>()
                });

                session.Chunks![chunkIndex] = chunkData;
                Logger.Write("RESTORE", $"Received chunk {chunkIndex + 1}/{totalChunks} for {restoreId}");

                if (session.Chunks.Count == totalChunks)
                {
                    Logger.Write("RESTORE", $"All chunks received: {session.Chunks.Count}/{totalChunks} for {restoreId}");

                    foreach (var kvp in session.Chunks.OrderBy(kvp => kvp.Key))
                    {
                        Logger.Write("RESTORE", $"Chunk {kvp.Key}: {kvp.Value.Substring(0, Math.Min(100, kvp.Value.Length))}...");
                    }

                    try
                    {
                        var orderedChunks = session.Chunks.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value);
                        string fullSqlBase64 = string.Concat(orderedChunks);
                        byte[] decodedBytes = Convert.FromBase64String(fullSqlBase64);
                        string fullSql = Encoding.UTF8.GetString(decodedBytes);

                        Logger.Write("RESTORE", $"Full SQL Content: {fullSql}");

                        Stopwatch stopwatch = Stopwatch.StartNew();
                        bool success = ExecuteRestoreSql(fullSql);
                        stopwatch.Stop();

                        double executionTime = stopwatch.Elapsed.TotalSeconds;
                        Logger.Write("RESTORE", $"Restore completed in {executionTime:F2} seconds");

                        _restoreSessions.TryRemove(restoreId, out _);

                        return success
                            ? CreateSuccessResponse($"Database restored successfully in {executionTime:F2} seconds")
                            : CreateErrorResponse("SQL execution failed during restore");
                    }
                    catch (Exception ex)
                    {
                        _restoreSessions.TryRemove(restoreId, out _);
                        Logger.Write("RESTORE", $"Restore {restoreId} failed: {ex.Message}");
                        return CreateErrorResponse($"Restore failed: {ex.Message}");
                    }
                }

                return CreateSuccessResponse($"Chunk {chunkIndex + 1}/{totalChunks} received");
            }
            catch (Exception ex)
            {
                Logger.Write("RESTORE", $"Error in restore handler: {ex.Message}");
                return CreateErrorResponse($"Internal server error: {ex.Message}");
            }
        }

        private bool ExecuteRestoreSql(string sql)
        {
            using var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString);
            connection.Open();

            Logger.Write("RESTORE", "First part of SQL: " + sql.Substring(0, Math.Min(sql.Length, 1000)));

            using var transaction = connection.BeginTransaction();

            try
            {
                using (var cmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 0;", connection, transaction))
                {
                    cmd.ExecuteNonQuery();
                }

                var parser = new MySqlScript(connection, sql)
                {
                    Delimiter = ";"
                };

                parser.StatementExecuted += (sender, args) =>
                {
                    Logger.Write("RESTORE", $"Executed statement: {args.StatementText}");
                };

                parser.Error += (sender, args) =>
                {
                    Logger.Write("RESTORE", $"SQL Error: {args.Exception.Message}");
                    throw args.Exception;
                };

                int count = parser.Execute();
                Logger.Write("RESTORE", $"Executed {count} SQL statements");

                using (var cmd = new MySqlCommand("SET FOREIGN_KEY_CHECKS = 1;", connection, transaction))
                {
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Write("RESTORE", $"SQL execution failed: {ex.Message}");
                transaction.Rollback();
                return false;
            }
        }

        private Packet CreateSuccessResponse(string message) => new Packet
        {
            Type = PacketType.RestoreDataResponse,
            Success = true,
            Message = message,
            Data = new Dictionary<string, string>
            {
                { "success", "true" },
                { "message", message }
            }
        };

        private Packet CreateErrorResponse(string message) => new Packet
        {
            Type = PacketType.RestoreDataResponse,
            Success = false,
            Message = message,
            Data = new Dictionary<string, string>
            {
                { "success", "false" },
                { "message", message }
            }
        };

        private class RestoreSession
        {
            public string? Id { get; set; }
            public int TotalChunks { get; set; }
            public ConcurrentDictionary<int, string>? Chunks { get; set; }
        }
    }
}
