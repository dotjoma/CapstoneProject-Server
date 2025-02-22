using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using client.Helpers;
using Microsoft.VisualBasic.ApplicationServices;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Asn1.Ocsp;
using server.Core.Network;
using server.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using server.Models;

namespace server.Controllers
{
    public class UnitController
    {
        public Packet Create(Packet packet)
        {
            var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString);

            try
            {
                connection.Open();
                string unitName = packet.Data["unitName"];
                string unitDescription = packet.Data["unitDescription"];

                string checkQuery = "SELECT COUNT(*) FROM unit WHERE unitName = @unitName";
                using (var checkCommand = new MySqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@unitName", unitName.Trim());
                    int unitCount = Convert.ToInt32(checkCommand.ExecuteScalar());

                    if (unitCount > 0)
                    {
                        return new Packet
                        {
                            Type = PacketType.RegisterResponse,
                            Success = false,
                            Message = "Unit already exists",
                            Data = new Dictionary<string, string>
                            {
                                { "success", "false" },
                                { "message", "Unit already exists" }
                            }
                        };
                    }
                }

                string insertQuery = "INSERT INTO unit (unitName, unitDescription) VALUES (@unitName, @unitDescription)";
                using (var command = new MySqlCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@unitName", unitName);
                    command.Parameters.AddWithValue("@unitDescription", unitDescription);
                    command.ExecuteNonQuery();
                }

                return new Packet
                {
                    Type = PacketType.CreateUnitResponse, 
                    Success = true,
                    Message = "Unit created successfully",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "true" },
                        { "message", $"Unit '{unitName}' created successfully" }
                    }
                };
            }
            catch (Exception ex)
            {
                Logger.Write("UNIT", $"Error creating unit: {ex.Message}");

                return new Packet
                {
                    Type = PacketType.CreateUnitResponse,
                    Success = false,
                    Message = "Error creating unit",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", "Internal server error" },
                        { "units", "[]" }
                    }
                };
            }
        }

        public Packet Get(Packet packet)
        {
            var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString);

            try
            {
                connection.Open();
                string query = "SELECT unitId, unitName, unitDescription FROM unit";

                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        var units = new List<Unit>();

                        while (reader.Read())
                        {
                            units.Add(new Unit
                            {
                                unitId = reader.GetInt32("unitId"), 
                                unitName = reader.GetString("unitName"),
                                unitDescription = reader.IsDBNull(reader.GetOrdinal("unitDescription"))
                                    ? null
                                    : reader.GetString("unitDescription") 
                            });
                        }

                        Logger.Write("UNIT", units.Count > 0
                            ? "Units retrieved successfully"
                            : "No units found in database");

                        return new Packet
                        {
                            Type = PacketType.GetUnitResponse,
                            Success = true,
                            Message = units.Count > 0
                                ? "Units retrieved successfully"
                                : "No units found",
                            Data = new Dictionary<string, string>
                            {
                                { "success", "true" },
                                { "message", units.Count > 0
                                    ? "Units retrieved successfully"
                                    : "No units found" },
                                { "units", System.Text.Json.JsonSerializer.Serialize(units) }
                            }
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write("UNIT", $"Error retrieving units: {ex.Message}");

                return new Packet
                {
                    Type = PacketType.GetUnitResponse, 
                    Success = false,
                    Message = "Error retrieving units",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", "Internal server error" },
                        { "units", "[]" }
                    }
                };
            }
        }
    }
}
