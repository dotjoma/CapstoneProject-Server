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

        public Packet CreateInventoryUnitType(Packet packet)
        {
            var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString);

            try
            {
                connection.Open();
                string unitTypeName = packet.Data["unitName"];
                string unitTypeSymbol = packet.Data["unitSymbol"];

                string checkQuery = "SELECT COUNT(*) FROM unittypes WHERE unit_name = @unitTypeName";
                using (var checkCommand = new MySqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@unitTypeName", unitTypeName.Trim());
                    int unitTypeCount = Convert.ToInt32(checkCommand.ExecuteScalar());

                    if (unitTypeCount > 0)
                    {
                        return new Packet
                        {
                            Type = PacketType.CreateUnitTypeResponse,
                            Success = false,
                            Message = "Unit type already exists",
                            Data = new Dictionary<string, string>
                            {
                                { "success", "false" },
                                { "message", "Unit type already exists" }
                            }
                        };
                    }
                }

                string insertQuery = "INSERT INTO unittypes (unit_name, unit_symbol) VALUES (@unitTypeName, @unitTypeSymbol)";
                using (var command = new MySqlCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@unitTypeName", unitTypeName);
                    command.Parameters.AddWithValue("@unitTypeSymbol", unitTypeSymbol);
                    command.ExecuteNonQuery();
                }

                return new Packet
                {
                    Type = PacketType.CreateUnitTypeResponse,
                    Success = true,
                    Message = "Unit type created successfully",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "true" },
                        { "message", $"Unit type '{unitTypeName}' created successfully" }
                    }
                };
            }
            catch (Exception ex)
            {
                Logger.Write("UNIT TYPE", $"Error creating unit type: {ex.Message}");

                return new Packet
                {
                    Type = PacketType.CreateUnitTypeResponse,
                    Success = false,
                    Message = "Error creating unit type",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", "Internal server error" },
                        { "units", "[]" }
                    }
                };
            }
        }

        public Packet GetInventoryUnitType(Packet packet)
        {
            var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString);

            try
            {
                connection.Open();
                string query = "SELECT type_id, unit_name, unit_symbol FROM unittypes";

                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        var unitTypes = new List<Dictionary<string, object>>();

                        while (reader.Read())
                        {
                            var unitType = new Dictionary<string, object>
                            {
                                { "type_id", reader.GetInt32("type_id") },
                                { "unit_name", reader.GetString("unit_name") },
                                { "unit_symbol", reader.GetString("unit_symbol") }
                            };

                            unitTypes.Add(unitType);
                        }

                        return new Packet
                        {
                            Type = PacketType.GetUnitTypeResponse,
                            Success = true,
                            Message = unitTypes.Count > 0
                                ? "Unit types retrieved successfully"
                                : "No unit types found",
                            Data = new Dictionary<string, string>
                            {
                                { "success", "true" },
                                { "message", unitTypes.Count > 0
                                    ? "Unit types retrieved successfully"
                                    : "No unit types found" },
                                { "unittypes", System.Text.Json.JsonSerializer.Serialize(unitTypes) }
                            }
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write("UNIT TYPE", $"Error retrieving unit types: {ex.Message}");

                return new Packet
                {
                    Type = PacketType.GetUnitTypeResponse,
                    Success = false,
                    Message = "Error retrieving unit types",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", "Internal server error" },
                        { "unittypes", "[]" }
                    }
                };
            }
        }

        public Packet CreateInventoryUnitMeasure(Packet packet)
        {
            var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString);

            try
            {
                connection.Open();
                string unitMeasureName = packet.Data["measureName"];
                string unitMeasureSymbol = packet.Data["measureSymbol"];

                string checkQuery = "SELECT COUNT(*) FROM unitmeasures WHERE name = @unitMeasureName";
                using (var checkCommand = new MySqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@unitMeasureName", unitMeasureName.Trim());
                    int unitMeasureCount = Convert.ToInt32(checkCommand.ExecuteScalar());

                    if (unitMeasureCount > 0)
                    {
                        return new Packet
                        {
                            Type = PacketType.CreateUnitMeasureResponse,
                            Success = false,
                            Message = "Unit measure already exists",
                            Data = new Dictionary<string, string>
                            {
                                { "success", "false" },
                                { "message", "Unit measure already exists" }
                            }
                        };
                    }
                }

                string insertQuery = "INSERT INTO unitmeasures (name, symbol) VALUES (@unitMeasureName, @unitMeasureSymbol)";
                using (var command = new MySqlCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@unitMeasureName", unitMeasureName);
                    command.Parameters.AddWithValue("@unitMeasureSymbol", unitMeasureSymbol);
                    command.ExecuteNonQuery();
                }

                return new Packet
                {
                    Type = PacketType.CreateUnitMeasureResponse,
                    Success = true,
                    Message = "Unit measure created successfully",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "true" },
                        { "message", $"Unit measure '{unitMeasureName}' created successfully" }
                    }
                };
            }
            catch (Exception ex)
            {
                Logger.Write("UNIT MEASURE", $"Error creating unit measure: {ex.Message}");

                return new Packet
                {
                    Type = PacketType.CreateUnitMeasureResponse,
                    Success = false,
                    Message = "Error creating unit measure",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", "Internal server error" },
                        { "units", "[]" }
                    }
                };
            }
        }

        public Packet GetInventoryUnitMeasure(Packet packet)
        {
            var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString);

            try
            {
                connection.Open();
                string query = "SELECT measure_id, name, symbol FROM unitmeasures";

                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        var unitMeasures = new List<Dictionary<string, object>>();

                        while (reader.Read())
                        {
                            var unitMeasure = new Dictionary<string, object>
                            {
                                { "measure_id", reader.GetInt32("measure_id") },
                                { "name", reader.GetString("name") },
                                { "symbol", reader.GetString("symbol") }
                            };

                            unitMeasures.Add(unitMeasure);
                        }

                        return new Packet
                        {
                            Type = PacketType.GetUnitMeasureResponse,
                            Success = true,
                            Message = unitMeasures.Count > 0
                                ? "Unit measures retrieved successfully"
                                : "No unit measures found",
                            Data = new Dictionary<string, string>
                            {
                                { "success", "true" },
                                { "message", unitMeasures.Count > 0
                                    ? "Unit measures retrieved successfully"
                                    : "No unit measures found" },
                                { "unitmeasures", System.Text.Json.JsonSerializer.Serialize(unitMeasures) }
                            }
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write("UNIT MEASURE", $"Error retrieving unit measures: {ex.Message}");

                return new Packet
                {
                    Type = PacketType.GetUnitMeasureResponse,
                    Success = false,
                    Message = "Error retrieving unit measures",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", "Internal server error" },
                        { "unitmeasures", "[]" }
                    }
                };
            }
        }
    }
}
