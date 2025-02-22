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
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using System.Data;

namespace server.Controllers
{
    public class ProductController
    {
        public Packet Create(Packet request)
        {
            try
            {
                Logger.Write("PRODUCT CREATION", $"Processing product creation: {request.Data["pName"]}");

                string catId = request.Data["catId"];
                string scId = request.Data["scId"];
                string pName = request.Data["pName"];
                string pDesc = request.Data["pDesc"];
                string unitId = request.Data["unitId"];
                string unitPrice = request.Data["unitPrice"];
                string image = request.Data["image"];

                Packet? validationResult = ProductCreateValidation(request);
                if (validationResult != null)
                {
                    return validationResult;
                }

                string query = @"
                    INSERT INTO product (catId, scId, pName, pDesc, unitId, unitPrice, image)
                    VALUES (@catId, @scId, @pName, @pDesc, @unitId, @unitPrice, @image)";

                using (var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString))
                {
                    connection.Open();
                    Logger.Write("REGISTRATION", "Database connection opened");

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.Add("@catId", MySqlDbType.VarChar).Value = catId;
                        command.Parameters.Add("@scId", MySqlDbType.VarChar).Value = scId;
                        command.Parameters.Add("@pName", MySqlDbType.VarChar).Value = pName;
                        command.Parameters.Add("@pDesc", MySqlDbType.VarChar).Value = pDesc;
                        command.Parameters.Add("@unitId", MySqlDbType.VarChar).Value = unitId;
                        command.Parameters.Add("@unitPrice", MySqlDbType.Decimal).Value = decimal.Parse(unitPrice);
                        command.Parameters.Add("@image", MySqlDbType.VarChar).Value = image;

                        command.ExecuteNonQuery();
                    }

                    Logger.Write("PRODUCT CREATION", $"Successfully created new product: {pName}");

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
                string query = @"SELECT pId, pName, pDesc, unitPrice, image FROM product";

                using (var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString))
                {
                    connection.Open();
                    Logger.Write("GET PRODUCT", "Database connection opened");

                    var productsList = new List<Product>();

                    using (var command = new MySqlCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string? imageBase64 = null;
                            if (!reader.IsDBNull(reader.GetOrdinal("image")))
                            {
                                imageBase64 = reader.GetString("image");
                            }

                            var product = new Product
                            {
                                productId = reader.GetInt32("pId"),
                                productName = reader.GetString("pName"),
                                productDesc = reader.IsDBNull(reader.GetOrdinal("pDesc"))
                                    ? null
                                    : reader.GetString("pDesc"),
                                productPrice = reader.GetDecimal("unitPrice"),
                                productImage = imageBase64
                            };
                            productsList.Add(product);
                        }
                    }

                    Logger.Write("GET PRODUCT", $"Retrieved {productsList.Count} products from database");

                    return new Packet
                    {
                        Type = PacketType.GetProductResponse,
                        Success = true,
                        Message = "Products retrieved successfully",
                        Data = new Dictionary<string, string>
                        {
                            { "success", "true" },
                            { "message", "Products retrieved successfully" },
                            { "products", SerializeWithNewline(productsList) }
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.Write("PRODUCT REQUEST", $"Product request error: {ex.Message}");
                return new Packet
                {
                    Type = PacketType.GetProductResponse,
                    Success = false,
                    Message = "Internal server error",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", ex.Message }
                    }
                };
            }
        }

        private string SerializeWithNewline<T>(T data)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultBufferSize = 104857600,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            return System.Text.Json.JsonSerializer.Serialize(data, options) + "\n";
        }

        private Packet? ProductCreateValidation(Packet request)
        {
            string catId = request.Data["catId"];
            string scId = request.Data["scId"];
            string pName = request.Data["pName"];
            string pDesc = request.Data["pDesc"];
            string unitId = request.Data["unitId"];
            string unitPrice = request.Data["unitPrice"];
            string image = request.Data["image"];

            var responsePacket = new Packet
            {
                Type = PacketType.CreateProductResponse,
                Data = new Dictionary<string, string>()
            };

            // Validate product name
            if (string.IsNullOrWhiteSpace(pName))
            {
                responsePacket.Data["name"] = "Product name is required.";
            }

            // Validate price
            if (string.IsNullOrWhiteSpace(unitPrice))
            {
                responsePacket.Data["unitPrice"] = "Unit price is required.";
            }
            else if (!decimal.TryParse(unitPrice, out decimal priceValue) || priceValue <= 0)
            {
                responsePacket.Data["unitPrice"] = "Unit price must be a valid positive number.";
            }

            // Validate category
            if (string.IsNullOrWhiteSpace(catId) || catId == "Select Category")
            {
                responsePacket.Data["category"] = "Please select a category.";
            }

            // Validate subcategory
            if (string.IsNullOrWhiteSpace(scId) || scId == "Select SubCategory")
            {
                responsePacket.Data["subcategory"] = "Please select a subcategory.";
            }

            // Validate image
            if (string.IsNullOrWhiteSpace(image))
            {
                responsePacket.Data["image"] = "Please select an image.";
            }

            // Validate description
            if (string.IsNullOrWhiteSpace(pDesc))
            {
                responsePacket.Data["description"] = "Product description is required.";
            }

            // Return validation errors if any
            if (responsePacket.Data.Count > 0)
            {
                return responsePacket;
            }

            request.Data["pName"] = pName;
            request.Data["image"] = image;
            request.Data["unitPrice"] = unitPrice;
            request.Data["pDesc"] = pDesc;
            request.Data["catId"] = catId;
            request.Data["scId"] = scId;

            return null;
        }
    }
}
