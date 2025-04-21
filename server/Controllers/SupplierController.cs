using client.Helpers;
using MySql.Data.MySqlClient;
using server.Core.Network;
using server.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.Controllers
{
    public class SupplierController
    {
        public Packet CreateInventorySupplier(Packet packet)
        {
            var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString);

            try
            {
                connection.Open();
                string supplierName = packet.Data["supplierName"];
                string contactPerson = packet.Data["contactPerson"];
                string phone = packet.Data["phone"];
                string email = packet.Data["email"];
                string address = packet.Data["address"];
                string isActive = packet.Data["isActive"];

                // Check if supplier already exists
                string checkQuery = "SELECT COUNT(*) FROM suppliers WHERE supplier_name = @supplierName";
                using (var checkCommand = new MySqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@supplierName", supplierName.Trim());
                    int supplierCount = Convert.ToInt32(checkCommand.ExecuteScalar());

                    if (supplierCount > 0)
                    {
                        return new Packet
                        {
                            Type = PacketType.CreateSupplierResponse,
                            Success = false,
                            Message = "Supplier already exists",
                            Data = new Dictionary<string, string>
                            {
                                { "success", "false" },
                                { "message", "Supplier already exists" }
                            }
                        };
                    }
                }

                // Insert new supplier
                string insertQuery = @"
                INSERT INTO suppliers 
                    (supplier_name, contact_person, phone, email, address, is_active) 
                VALUES 
                (@supplierName, @contactPerson, @phone, @email, @address, @isActive)";
                using (var command = new MySqlCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@supplierName", supplierName);
                    command.Parameters.AddWithValue("@contactPerson", contactPerson);
                    command.Parameters.AddWithValue("@phone", phone);
                    command.Parameters.AddWithValue("@email", email);
                    command.Parameters.AddWithValue("@address", address);
                    command.Parameters.AddWithValue("@isActive", isActive);
                    command.ExecuteNonQuery();
                }

                return new Packet
                {
                    Type = PacketType.CreateSupplierResponse,
                    Success = true,
                    Message = "Supplier created successfully",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "true" },
                        { "message", $"Supplier '{supplierName}' created successfully" }
                    }
                };
            }
            catch (Exception ex)
            {
                Logger.Write("SUPPLIER", $"Error creating supplier: {ex.Message}");

                return new Packet
                {
                    Type = PacketType.CreateSupplierResponse,
                    Success = false,
                    Message = "Error creating supplier",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", "Internal server error" }
                    }
                };
            }
        }

        public Packet GetInventorySupplier(Packet packet)
        {
            var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString);

            try
            {
                connection.Open();
                string query = "SELECT supplier_id, supplier_name, contact_person, phone, email, address, is_active FROM suppliers";

                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        var suppliers = new List<Dictionary<string, object>>();

                        while (reader.Read())
                        {
                            var supplier = new Dictionary<string, object>
                            {
                                { "supplier_id", reader["supplier_id"] != DBNull.Value ? Convert.ToInt32(reader["supplier_id"]) : 0 },
                                { "supplier_name", reader["supplier_name"]?.ToString() ?? string.Empty },
                                { "contact_person", reader["contact_person"]?.ToString() ?? string.Empty },
                                { "phone", reader["phone"]?.ToString() ?? string.Empty },
                                { "email", reader["email"]?.ToString() ?? string.Empty },
                                { "address", reader["address"]?.ToString() ?? string.Empty },
                                { "is_active", reader["is_active"] != DBNull.Value ? Convert.ToInt32(reader["is_active"]) : 0 }
                            };

                            suppliers.Add(supplier);
                        }

                        bool hasData = suppliers.Count > 0;

                        return new Packet
                        {
                            Type = PacketType.GetSupplierResponse,
                            Success = true,
                            Message = hasData
                                ? "Suppliers retrieved successfully"
                                : "No suppliers found",
                            Data = new Dictionary<string, string>
                            {
                                { "success", hasData.ToString().ToLower() },
                                { "message", hasData
                                    ? "Suppliers retrieved successfully"
                                    : "No suppliers found" },
                                { "suppliers", System.Text.Json.JsonSerializer.Serialize(suppliers) }
                            }
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write("SUPPLIER", $"Error retrieving suppliers: {ex.Message}");

                return new Packet
                {
                    Type = PacketType.GetSupplierResponse,
                    Success = false,
                    Message = "Error retrieving suppliers",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", "Internal server error" },
                        { "suppliers", "[]" }
                    }
                };
            }
        }
    }
}
