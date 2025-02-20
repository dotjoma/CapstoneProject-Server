using client.Helpers;
using MySql.Data.MySqlClient;
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
    public class SubCategoryController
    {
        public Packet Get(Packet packet)
        {
            if (!packet.Data.ContainsKey("catId"))
            {
                Logger.Write("SUBCATEGORY", "catId key not found in packet data");
                return new Packet
                {
                    Type = PacketType.GetSubCategoryResponse,
                    Success = false,
                    Message = "Missing category ID",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", "Missing category ID" },
                        { "subcategories", "[]" }
                    }
                };
            }

            if (!int.TryParse(packet.Data["catId"], out int categoryId))
            {
                Logger.Write("SUBCATEGORY", $"Failed to parse catId value: {packet.Data["catId"]}");
                return new Packet
                {
                    Type = PacketType.GetSubCategoryResponse,
                    Success = false,
                    Message = "Invalid category ID format",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", "Invalid category ID format" },
                        { "subcategories", "[]" }
                    }
                };
            }

            try
            {
                using (var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString))
                {
                    connection.Open();
                    string query = @"SELECT scId, scName, catId FROM subcategory WHERE catId = @categoryId";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@categoryId", categoryId);

                        using (var reader = command.ExecuteReader())
                        {
                            var subcategories = new List<SubCategory>();

                            while (reader.Read())
                            {
                                subcategories.Add(new SubCategory
                                {
                                    scId = reader.GetInt32("scId"),
                                    catId = reader.GetInt32("catId"),
                                    scName = reader.GetString("scName")
                                });
                            }

                            Logger.Write("SUBCATEGORY", subcategories.Count > 0
                                ? $"Found {subcategories.Count} subcategories for category {categoryId}"
                                : $"No subcategories found for category {categoryId}");

                            return new Packet
                            {
                                Type = PacketType.GetSubCategoryResponse,
                                Success = true,
                                Message = subcategories.Count > 0
                                    ? "Subcategories retrieved successfully"
                                    : "No subcategories found",
                                Data = new Dictionary<string, string>
                                {
                                    { "success", "true" },
                                    { "message", subcategories.Count > 0
                                        ? "Subcategories retrieved successfully"
                                        : "No subcategories found" },
                                    { "subcategories", System.Text.Json.JsonSerializer.Serialize(subcategories) }
                                }
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write("SUBCATEGORY", $"Error retrieving subcategories: {ex.Message}");
                Logger.Write("SUBCATEGORY", $"Stack trace: {ex.StackTrace}");

                return new Packet
                {
                    Type = PacketType.GetSubCategoryResponse,
                    Success = false,
                    Message = "Error retrieving subcategories",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", $"Internal server error: {ex.Message}" },
                        { "subcategories", "[]" }
                    }
                };
            }
        }

        public Packet Create(Packet packet)
        {
            try
            {
                using (var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString))
                {
                    connection.Open();
                    string subCategoryName = packet.Data["name"];
                    int categoryId = int.Parse(packet.Data["categoryId"]);

                    // Check if subcategory name exists in the same category
                    string checkQuery = "SELECT COUNT(*) FROM subcategory WHERE scName = @scName AND catId = @catId";
                    using (var checkCommand = new MySqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@scName", subCategoryName.Trim());
                        checkCommand.Parameters.AddWithValue("@catId", categoryId);
                        int count = Convert.ToInt32(checkCommand.ExecuteScalar());

                        if (count > 0)
                        {
                            return new Packet
                            {
                                Type = PacketType.CreateSubCategoryResponse,
                                Success = false,
                                Message = "Sub-category already exists in this category",
                                Data = new Dictionary<string, string>
                                {
                                    { "success", "false" },
                                    { "message", "Sub-category already exists in this category" }
                                }
                            };
                        }
                    }


                    // Check if category exists
                    string checkCategoryQuery = "SELECT COUNT(*) FROM category WHERE catId = @catId";
                    using (var checkCatCommand = new MySqlCommand(checkCategoryQuery, connection))
                    {
                        checkCatCommand.Parameters.AddWithValue("@catId", categoryId);
                        int catCount = Convert.ToInt32(checkCatCommand.ExecuteScalar());

                        if (catCount == 0)
                        {
                            return new Packet
                            {
                                Type = PacketType.CreateSubCategoryResponse,
                                Success = false,
                                Message = "Parent category does not exist",
                                Data = new Dictionary<string, string>
                                {
                                    { "success", "false" },
                                    { "message", "Parent category does not exist" }
                                }
                            };
                        }
                    }

                    // Insert the subcategory
                    string insertQuery = "INSERT INTO subcategory (scName, catId) VALUES (@scName, @catId)";
                    using (var command = new MySqlCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@scName", subCategoryName);
                        command.Parameters.AddWithValue("@catId", categoryId);
                        command.ExecuteNonQuery();
                    }

                    return new Packet
                    {
                        Type = PacketType.CreateSubCategoryResponse,
                        Success = true,
                        Message = "Sub-category created successfully",
                        Data = new Dictionary<string, string>
                        {
                            { "success", "true" },
                            { "message", $"Sub-category '{subCategoryName}' created successfully" }
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.Write("SUBCATEGORY", $"Error creating sub-category: {ex.Message}");

                return new Packet
                {
                    Type = PacketType.CreateSubCategoryResponse,
                    Success = false,
                    Message = "Error creating sub-category",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", "Internal server error" }
                    }
                };
            }
        }
    }
}
