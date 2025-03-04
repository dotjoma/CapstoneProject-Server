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
    public class DiscountController
    {
        public Packet Get(Packet packet)
        {
            var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString);

            try
            {
                connection.Open();
                string query = "SELECT id, name, type, value, vat_exempt, status FROM discounts";

                var discounts = new List<Discount>();

                using (var command = new MySqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        discounts.Add(new Discount
                        {
                            Id = reader.GetInt32("id"),
                            Name = reader.GetString("name"),
                            Type = reader.GetString("type"),
                            Value = reader.GetDecimal("value"),
                            VatExempt = reader.GetInt32("vat_exempt"),
                            Status = reader.GetInt32("status"),
                            ApplicableTo = ""
                        });
                    }
                }

                string applicableToQuery = "SELECT discount_id, GROUP_CONCAT(category_id) as category_ids " +
                                         "FROM discount_category " +
                                         "GROUP BY discount_id";

                using (var applicableToCommand = new MySqlCommand(applicableToQuery, connection))
                using (var applicableToReader = applicableToCommand.ExecuteReader())
                {
                    while (applicableToReader.Read())
                    {
                        int discountId = applicableToReader.GetInt32("discount_id");
                        string categoryIds = applicableToReader.GetString("category_ids");

                        var discount = discounts.FirstOrDefault(d => d.Id == discountId);
                        if (discount != null)
                        {
                            discount.ApplicableTo = categoryIds;
                        }
                    }
                }

                Logger.Write("DISCOUNT", discounts.Count > 0
                        ? "Discounts retrieved successfully"
                        : "No discounts found in database");

                return new Packet
                {
                    Type = PacketType.GetDiscountResponse,
                    Success = true,
                    Message = discounts.Count > 0
                        ? "Discounts retrieved successfully"
                        : "No discounts found",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "true" },
                        { "message", discounts.Count > 0
                            ? "Discounts retrieved successfully"
                            : "No discounts found" },
                        { "discounts", System.Text.Json.JsonSerializer.Serialize(discounts) }
                    }
                };
            }
            catch (Exception ex)
            {
                Logger.Write("DISCOUNT", $"Error retrieving discounts: {ex.Message}");

                return new Packet
                {
                    Type = PacketType.GetDiscountResponse,
                    Success = false,
                    Message = "Error retrieving discounts",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", "Internal server error" },
                        { "discounts", "[]" }
                    }
                };
            }
        }

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

                Logger.Write("DISCOUNT CREATION", $"Raw categoryIds value: {request.Data["categoryIds"]}");

                List<int> categoryIds = new List<int>();
                if (request.Data.ContainsKey("categoryIds") && !string.IsNullOrEmpty(request.Data["categoryIds"]))
                {
                    var rawIds = request.Data["categoryIds"]
                        .Trim('[', ']', ' ')
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .ToList(); 

                    Logger.Write("DISCOUNT CREATION", $"Split values: {string.Join("|", rawIds)}");

                    foreach (var id in rawIds)
                    {
                        if (int.TryParse(id, out int parsedId))
                        {
                            categoryIds.Add(parsedId);
                        }
                        else
                        {
                            Logger.Write("DISCOUNT CREATION", $"Failed to parse category ID: '{id}'");
                        }
                    }
                }

                Logger.Write("DISCOUNT CREATION", $"Final categoryIds: {string.Join(", ", categoryIds)}");

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
                        string insertCategoryQuery = "INSERT INTO discount_category (discount_id, category_id) VALUES (@discountId, @categoryId)";

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
                var categoryIds = categoryIdsStr
                    .Trim('[', ']')
                    .Split(',')
                    .Select(id => id.Trim())
                    .ToList();

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
