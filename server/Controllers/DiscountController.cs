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
    public class DiscountController
    {
        public Packet Create(Packet request)
        {
            try
            {
                Logger.Write("DISCOUNT CREATION", $"Processing discount creation: {request.Data["name"]}");

                string name = request.Data["name"];
                string type = request.Data["type"];
                decimal value = decimal.Parse(request.Data["value"]);
                int vatExempt = int.Parse(request.Data["vatExempt"]);
                int status = int.Parse(request.Data["status"]);

                List<int> categoryIds = request.Data.ContainsKey("categoryIds")
                    ? request.Data["categoryIds"].Trim('[', ']').Split(',').Select(int.Parse).ToList() // Remove brackets before splitting
                    : new List<int>();

                MessageBox.Show($"Category IDs: {string.Join(", ", categoryIds)}");

                Packet? validationResult = DiscountCreateValidation(request);
                if (validationResult != null)
                {
                    return validationResult;
                }

                using (var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString))
                {
                    connection.Open();
                    Logger.Write("DISCOUNT CREATION", "Database connection opened");

                    string insertDiscountQuery = @"
                        INSERT INTO discounts (name, type, value, vat_exempt, status) 
                        VALUES (@name, @type, @value, @vatExempt, @status);
                        SELECT LAST_INSERT_ID();";

                    int discountId;
                    using (var command = new MySqlCommand(insertDiscountQuery, connection))
                    {
                        command.Parameters.AddWithValue("@name", name);
                        command.Parameters.AddWithValue("@type", type);
                        command.Parameters.AddWithValue("@value", value);
                        command.Parameters.AddWithValue("@vatExempt", vatExempt);
                        command.Parameters.AddWithValue("@status", status);

                        discountId = Convert.ToInt32(command.ExecuteScalar());
                    }

                    if (categoryIds.Count > 0)
                    {
                        string insertCategoryQuery = "INSERT INTO discount_categories (discount_id, category_id) VALUES (@discountId, @categoryId)";

                        foreach (int categoryId in categoryIds)
                        {
                            using (var categoryCommand = new MySqlCommand(insertCategoryQuery, connection))
                            {
                                categoryCommand.Parameters.AddWithValue("@discountId", discountId);
                                categoryCommand.Parameters.AddWithValue("@categoryId", categoryId);
                                categoryCommand.ExecuteNonQuery();
                            }
                        }
                    }

                    Logger.Write("DISCOUNT CREATION", $"Successfully created new discount: {name} (ID: {discountId})");

                    return new Packet
                    {
                        Type = PacketType.CreateDiscountResponse,
                        Success = true,
                        Message = "New discount created successfully",
                        Data = new Dictionary<string, string>
                        {
                            { "success", "true" },
                            { "message", "Discount creation successful" },
                            { "discountId", discountId.ToString() }
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.Write("DISCOUNT CREATION", $"Error creating discount: {ex.Message}");

                return new Packet
                {
                    Type = PacketType.CreateDiscountResponse,
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


        private Packet? DiscountCreateValidation(Packet request)
        {
            var responsePacket = new Packet
            {
                Type = PacketType.CreateDiscountResponse,
                Data = new Dictionary<string, string>()
            };

            if (!request.Data.TryGetValue("name", out string? name) || string.IsNullOrWhiteSpace(name))
            {
                responsePacket.Data["name"] = "Discount name is required.";
            }

            if (!request.Data.TryGetValue("type", out string? type) || string.IsNullOrWhiteSpace(type))
            {
                responsePacket.Data["type"] = "Discount type is required.";
            }
            else if (type.ToLower() != "percentage" && type.ToLower() != "fixed")
            {
                responsePacket.Data["type"] = "Discount type must be 'percentage' or 'fixed'.";
            }

            if (!request.Data.TryGetValue("value", out string? valueStr) || string.IsNullOrWhiteSpace(valueStr))
            {
                responsePacket.Data["value"] = "Discount value is required.";
            }
            else if (!decimal.TryParse(valueStr, out decimal value) || value <= 0)
            {
                responsePacket.Data["value"] = "Discount value must be a valid positive number.";
            }

            if (!request.Data.TryGetValue("vatExempt", out string? vatExemptStr) || string.IsNullOrWhiteSpace(vatExemptStr))
            {
                responsePacket.Data["vatExempt"] = "VAT exemption status is required.";
            }
            else if (!int.TryParse(vatExemptStr, out int vatExempt) || (vatExempt != 0 && vatExempt != 1))
            {
                responsePacket.Data["vatExempt"] = "VAT exemption must be 0 (no) or 1 (yes).";
            }

            if (!request.Data.TryGetValue("status", out string? statusStr) || string.IsNullOrWhiteSpace(statusStr))
            {
                responsePacket.Data["status"] = "Discount status is required.";
            }
            else if (!int.TryParse(statusStr, out int status) || (status != 0 && status != 1))
            {
                responsePacket.Data["status"] = "Status must be 0 (inactive) or 1 (active).";
            }

            if (request.Data.TryGetValue("categoryIds", out string? categoryIdsStr) && !string.IsNullOrWhiteSpace(categoryIdsStr))
            {
                var categoryIds = categoryIdsStr.Split(',').Select(id => id.Trim()).ToList();
                if (!categoryIds.All(id => int.TryParse(id, out _)))
                {
                    responsePacket.Data["categoryIds"] = "Invalid category IDs.";
                }
            }
            else
            {
                responsePacket.Data["categoryIds"] = "At least one category must be selected.";
            }

            if (responsePacket.Data.Count > 0)
            {
                return responsePacket;
            }

            return null;
        }
    }
}
