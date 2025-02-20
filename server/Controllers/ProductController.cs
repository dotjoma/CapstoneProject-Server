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
    public class ProductController
    {
        public Packet Create(Packet request)
        {
            try
            {
                Logger.Write("PRODUCT CREATION", $"Processing product creation: {request.Data["name"]}");

                string name = request.Data["name"];
                string image = request.Data["image"];
                string price = request.Data["price"];
                string description = request.Data["description"];
                string category = request.Data["category"];

                Packet? validationResult = ProductCreateValidation(request, name, image, price, description, category);

                string query = @"
                        INSERT INTO products (name, image, price, description, category, created_at, updated_at)
                        VALUES (@name, @image, @price, @description, @category, @created_at, @updated_at)";

                using (var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString))
                {
                    connection.Open();
                    Logger.Write("REGISTRATION", "Database connection opened");

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@name", name);
                        command.Parameters.AddWithValue("@image", image);
                        command.Parameters.AddWithValue("@price", price);
                        command.Parameters.AddWithValue("@description", description);
                        command.Parameters.AddWithValue("@category", category);
                        command.Parameters.AddWithValue("@created_at", DateTime.Now);
                        command.Parameters.AddWithValue("@updated_at", DateTime.Now);
                        command.ExecuteNonQuery();
                    }

                    Logger.Write("PRODUCT CREATION", $"\"Successfully created new product: {name}\"");

                    return new Packet
                    {
                        Type = PacketType.CreateProductResponse,
                        Success = true,
                        Message = "New product created",
                        Data = new Dictionary<string, string>
                        {
                            { "success", "true" },
                            { "message", "Create product successful" }
                        }
                    };
                }
            }
            catch (Exception ex)
            { 
                Logger.Write("PRODUCT CREATION", $"Product creation error: {ex.Message}");

                return new Packet
                {
                    Type = PacketType.CreateProductResponse,
                    Success = false,
                    Message = "Internal server error",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", "Internal server error" }
                    }
                };
            }
        }

        public Packet Get(Packet request)
        {
            try
            {
                string query = @"SELECT catId, catName FROM category";

                using (var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString))
                {
                    connection.Open();
                    Logger.Write("GET PRODUCT", "Database connection opened");

                    using (var command = new MySqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {

                            }

                            int catId = reader.GetInt32("catId");
                            string catName = reader.GetString("catName");

                            return new Packet
                            {
                                Type = PacketType.LoginResponse,
                                Success = true,
                                Message = "Retrieved products successful",
                                Data = new Dictionary<string, string>
                                {
                                    { "success", "true" },
                                    { "message", "Retrieved products successful" },
                                    { "catId", catId.ToString() },
                                    { "catName", catName }
                                }
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write("PRODUCT REQUEST", $"Product request error: {ex.Message}");

                return new Packet
                {
                    Type = PacketType.CreateProductResponse,
                    Success = false,
                    Message = "Internal server error",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", "Internal server error" }
                    }
                };
            }
        }

        private Packet? ProductCreateValidation(Packet request, string name, string image, string price, string description, string category)
        {
            var responsePacket = new Packet
            {
                Type = PacketType.CreateProductResponse,
                Data = new Dictionary<string, string>()
            };

            // Validate product name
            if (string.IsNullOrWhiteSpace(name))
            {
                responsePacket.Data["name"] = "Product name is required.";
            }

            // Validate price
            if (string.IsNullOrWhiteSpace(price))
            {
                responsePacket.Data["price"] = "Price is required.";
            }
            else if (!decimal.TryParse(price, out decimal priceValue) || priceValue <= 0)
            {
                responsePacket.Data["price"] = "Price must be a valid positive number.";
            }

            // Validate category
            if (string.IsNullOrWhiteSpace(category) || category == "Select Category") // Adjust based on your category selection logic
            {
                responsePacket.Data["category"] = "Please select a category.";
            }

            // Validate image
            if (string.IsNullOrWhiteSpace(image))
            {
                responsePacket.Data["image"] = "Please select an image.";
            }

            if (responsePacket.Data.Count > 0)
            {
                return responsePacket;
            }

            request.Data["name"] = name;
            request.Data["image"] = image;
            request.Data["price"] = price;
            request.Data["description"] = description;
            request.Data["category"] = category;

            return null;
        }
    }
}
