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
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using server.Models;

namespace server.Controllers
{
    public class CategoryController
    {
        public Packet Create(Packet packet)
        {
            var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString);

            try
            {
                connection.Open();
                string categoryName = packet.Data["name"];

                // Check if category name exists
                string checkQuery = "SELECT COUNT(*) FROM category WHERE catName = @catName";
                using (var checkCommand = new MySqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@catName", categoryName.Trim());
                    int userCount = Convert.ToInt32(checkCommand.ExecuteScalar());

                    if (userCount > 0)
                    {
                        return new Packet
                        {
                            Type = PacketType.RegisterResponse,
                            Success = false,
                            Message = "Category already exists",
                            Data = new Dictionary<string, string>
                                {
                                    { "success", "false" },
                                    { "message", "Category already exists" }
                                }
                        };
                    }
                }

                string insertQuery = "INSERT INTO category (catName) VALUES (@catName)";
                using (var command = new MySqlCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@catName", categoryName);
                    command.ExecuteNonQuery();
                }

                return new Packet
                {
                    Type = PacketType.CreateCategoryResponse,
                    Success = true,
                    Message = "Category created successfully",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "true" },
                        { "message", $"Category '{categoryName}' created successfully" }
                    }
                };
            }
            catch (Exception ex)
            {
                Logger.Write("CATEGORY", $"Error creating category: {ex.Message}");

                return new Packet
                {
                    Type = PacketType.CreateCategoryResponse,
                    Success = false,
                    Message = "Error creating category",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", "Internal server error" },
                        { "categories", "[]" }
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
                string query = "SELECT catId, catName FROM category";

                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        var categories = new List<Category>();

                        while (reader.Read())
                        {
                            categories.Add(new Category
                            {
                                Id = reader.GetInt32("catId"),
                                Name = reader.GetString("catName")
                            });
                        }

                        Logger.Write("CATEGORY", categories.Count > 0
                            ? "Categories retrieved successfully"
                            : "No categories found in database");

                        return new Packet
                        {
                            Type = PacketType.GetCategoryResponse,
                            Success = true,
                            Message = categories.Count > 0
                                ? "Categories retrieved successfully"
                                : "No categories found",
                            Data = new Dictionary<string, string>
                            {
                                { "success", "true" },
                                { "message", categories.Count > 0
                                    ? "Categories retrieved successfully"
                                    : "No categories found" },
                                { "categories", System.Text.Json.JsonSerializer.Serialize(categories) }
                            }
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write("CATEGORY", $"Error retrieving categories: {ex.Message}");

                return new Packet
                {
                    Type = PacketType.GetCategoryResponse,
                    Success = false,
                    Message = "Error retrieving categories",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", "Internal server error" },
                        { "categories", "[]" }
                    }
                };
            }
        }

        public Packet CreateInventoryCategory(Packet packet)
        {
            var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString);

            try
            {
                connection.Open();
                string categoryName = packet.Data["name"];

                // Check if category name exists
                string checkQuery = "SELECT COUNT(*) FROM inventory_categories WHERE category_name = @catName";
                using (var checkCommand = new MySqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@catName", categoryName.Trim());
                    int userCount = Convert.ToInt32(checkCommand.ExecuteScalar());

                    if (userCount > 0)
                    {
                        return new Packet
                        {
                            Type = PacketType.RegisterResponse,
                            Success = false,
                            Message = "Category already exists",
                            Data = new Dictionary<string, string>
                                {
                                    { "success", "false" },
                                    { "message", "Category already exists" }
                                }
                        };
                    }
                }

                string insertQuery = "INSERT INTO inventory_categories (category_name) VALUES (@catName)";
                using (var command = new MySqlCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@catName", categoryName);
                    command.ExecuteNonQuery();
                }

                return new Packet
                {
                    Type = PacketType.CreateCategoryResponse,
                    Success = true,
                    Message = "Category created successfully",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "true" },
                        { "message", $"Category '{categoryName}' created successfully" }
                    }
                };
            }
            catch (Exception ex)
            {
                Logger.Write("CATEGORY", $"Error creating category: {ex.Message}");

                return new Packet
                {
                    Type = PacketType.CreateCategoryResponse,
                    Success = false,
                    Message = "Error creating category",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", "Internal server error" },
                        { "categories", "[]" }
                    }
                };
            }
        }

        public Packet GetInventoryCategory(Packet packet)
        {
            var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString);

            try
            {
                connection.Open();
                string query = "SELECT category_id, category_name FROM inventory_categories";

                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        var categories = new List<Dictionary<string, object>>();

                        while (reader.Read())
                        {
                            var category = new Dictionary<string, object>
                            {
                                { "category_id", reader.GetInt32("category_id") },
                                { "category_name", reader.GetString("category_name") }
                            };

                            categories.Add(category);
                        }

                        Logger.Write("CATEGORY", categories.Count > 0
                            ? "Categories retrieved successfully"
                            : "No categories found in database");

                        return new Packet
                        {
                            Type = PacketType.GetCategoryResponse,
                            Success = true,
                            Message = categories.Count > 0
                                ? "Categories retrieved successfully"
                                : "No categories found",
                            Data = new Dictionary<string, string>
                            {
                                { "success", "true" },
                                { "message", categories.Count > 0
                                    ? "Categories retrieved successfully"
                                    : "No categories found" },
                                { "categories", System.Text.Json.JsonSerializer.Serialize(categories) }
                            }
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write("CATEGORY", $"Error retrieving categories: {ex.Message}");

                return new Packet
                {
                    Type = PacketType.GetCategoryResponse,
                    Success = false,
                    Message = "Error retrieving categories",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", "Internal server error" },
                        { "categories", "[]" }
                    }
                };
            }
        }
    }
}
