using client.Helpers;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using server.Core.Network;
using server.Database;
using server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.Controllers
{
    public class AuditTrailController
    {
        public Packet SaveAudit(Packet request)
        {
            try
            {
                if (request.Data == null ||
                    !request.Data.ContainsKey("user_id") ||
                    !request.Data.ContainsKey("action_type"))
                {
                    return new Packet
                    {
                        Type = PacketType.AuditSaveResponse,
                        Success = false,
                        Data = new Dictionary<string, string>
                        {
                            { "success", "false" },
                            { "message", "Missing required audit fields" }
                        }
                    };
                }

                var idempotencyKey = $"{request.Data["user_id"]}-{request.Data["action_type"]}-" +
                                   $"{(request.Data.ContainsKey("entity_id") ? request.Data["entity_id"] : "null")}-" +
                                   $"{DateTime.UtcNow:yyyyMMddHHmm}";

                using (var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString))
                {
                    connection.Open();

                    using (var checkCmd = new MySqlCommand(
                        "SELECT 1 FROM auditlogs WHERE idempotency_key = @key LIMIT 1", connection))
                    {
                        checkCmd.Parameters.AddWithValue("@key", idempotencyKey);
                        if (checkCmd.ExecuteScalar() != null)
                        {
                            return new Packet
                            {
                                Type = PacketType.AuditSaveResponse,
                                Success = true,
                                Data = new Dictionary<string, string>
                                {
                                    { "success", "true" },
                                    { "message", "Duplicate audit detected - skipped" }
                                }
                            };
                        }
                    }

                    string sql = @"
                    INSERT INTO auditlogs 
                    (user_id, action_type, description, prev_value, new_value, 
                     ip_address, entity, entity_id, idempotency_key)
                    VALUES 
                    (@userId, @actionType, @description, @prevValue, @newValue,
                     @ipAddress, @entity, @entityId, @idempotencyKey)";

                    using (var cmd = new MySqlCommand(sql, connection))
                    {
                        cmd.Parameters.AddWithValue("@userId", request.Data["user_id"]);
                        cmd.Parameters.AddWithValue("@actionType", request.Data["action_type"]);
                        cmd.Parameters.AddWithValue("@description", request.Data["description"]);
                        cmd.Parameters.AddWithValue("@prevValue", request.Data["prev_value"]);
                        cmd.Parameters.AddWithValue("@newValue", request.Data["new_value"]);
                        cmd.Parameters.AddWithValue("@ipAddress", request.Data["ip_address"]);
                        cmd.Parameters.AddWithValue("@entity", request.Data["entity"]);
                        cmd.Parameters.AddWithValue("@entityId", request.Data["entity_id"]);
                        cmd.Parameters.AddWithValue("@idempotencyKey", idempotencyKey);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            return new Packet
                            {
                                Type = PacketType.AuditSaveResponse,
                                Success = true,
                                Data = new Dictionary<string, string>
                                {
                                    { "success", "true" },
                                    { "message", "Audit logged successfully" }
                                }
                            };
                        }
                    }
                }

                return new Packet
                {
                    Type = PacketType.AuditSaveResponse,
                    Success = false,
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", "No rows affected" }
                    }
                };
            }
            catch (Exception ex)
            {
                Logger.Write("AUDIT_TRAIL", ex.Message);
                return new Packet
                {
                    Type = PacketType.AuditSaveResponse,
                    Success = false,
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", "Failed to save audit log" }
                    }
                };
            }
        }

        public Packet GetAllAudit(Packet request)
        {
            const string logPrefix = "AUDIT_TRAIL";
            try
            {
                string query = @"SELECT 
                    a.id, 
                    a.date, 
                    a.user_id, 
                    a.action_type, 
                    a.description, 
                    a.prev_value, 
                    a.new_value, 
                    a.ip_address, 
                    a.entity, 
                    a.entity_id,
                    CONCAT(u.fname, ' ', u.lname) AS user_fullname
                FROM auditlogs a
                LEFT JOIN users u ON a.user_id = u.id
                ORDER BY a.date DESC";

                using (var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString))
                {
                    connection.Open();
                    Logger.Write(logPrefix, "Database connection opened");

                    var auditTrails = new List<Audit>();

                    using (var command = new MySqlCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var audit = new Audit
                            {
                                Id = reader.GetInt32("id"),
                                Date = reader.GetDateTime("date"),
                                Name = reader.GetString("user_fullname"),
                                Action = reader.GetString("action_type"),
                                Description = reader.GetString("description"),
                                PrevValue = reader.GetString("prev_value"),
                                NewValue = reader.GetString("new_value"),
                                IpAddress = reader.GetString("ip_address"),
                                Entity = reader.GetString("entity"),
                                EntityId = reader.GetInt32("entity_id")
                            };
                            auditTrails.Add(audit);
                        }
                    }

                    Logger.Write(logPrefix, $"Retrieved {auditTrails.Count} audit trails from database");

                    return new Packet
                    {
                        Type = PacketType.GetAuditResponse,
                        Success = true,
                        Message = "Audit trails retrieved successfully",
                        Data = new Dictionary<string, string>
                        {
                            { "success", "true" },
                            { "message", "Audit trails retrieved successfully" },
                            { "audits", JsonConvert.SerializeObject(auditTrails) }
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.Write(logPrefix, $"Error retrieving audit trails: {ex.Message}");
                return new Packet
                {
                    Type = PacketType.GetAuditResponse,
                    Success = false,
                    Message = "Error retrieving audit trails",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", ex.Message }
                    }
                };
            }
        }
    }
}
