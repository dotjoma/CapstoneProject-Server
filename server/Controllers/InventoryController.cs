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
    public class InventoryController
    {
        public Packet CreateInventoryItem(Packet packet)
        {
            using (var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string itemName = packet.Data["itemName"];
                        string categoryId = packet.Data["categoryId"];
                        string subcategoryId = packet.Data["subcategoryId"];
                        string batchNumber = packet.Data["batchNumber"];
                        string purchaseDate = packet.Data["purchaseDate"];
                        string expirationDate = packet.Data["expirationDate"];
                        string batchQuantity = packet.Data["batchQuantity"];
                        string unitTypeId = packet.Data["unitTypeId"];
                        string unitMeasureId = packet.Data["unitMeasureId"];
                        string minStock = packet.Data["minStock"];
                        string maxStock = packet.Data["maxStock"];
                        string reorderPoint = packet.Data["reorderPoint"];
                        string leadTime = packet.Data["leadTime"];
                        string turnOver = packet.Data["turnOver"];
                        string unitCost = packet.Data["unitCost"];
                        string? supplierId = packet.Data["supplierId"];
                        string enableLowStockAlert = packet.Data.ContainsKey("enableLowStockAlert") ? packet.Data["enableLowStockAlert"] : "0";
                        
                        long insertedItemId;

                        string insertItemQuery = @"
                        INSERT INTO inventory_items (
                            item_name, description, category_id, subcategory_id,
                            unit_type_id, unit_measure_id, min_stock_level, max_stock_level,
                            reorder_point, lead_time_days, target_turnover_days, enable_low_stock_alert
                        )
                        VALUES (
                            @itemName, '', @categoryId, @subcategoryId,
                            @unitTypeId, @unitMeasureId, @minStock, @maxStock,
                            @reorderPoint, @leadTime, @turnOver, @enableLowStockAlert
                        );
                        SELECT LAST_INSERT_ID();";

                        using (var insertItemCmd = new MySqlCommand(insertItemQuery, connection, transaction))
                        {
                            insertItemCmd.Parameters.AddWithValue("@itemName", itemName);
                            insertItemCmd.Parameters.AddWithValue("@categoryId", categoryId);
                            insertItemCmd.Parameters.AddWithValue("@subcategoryId", subcategoryId);
                            insertItemCmd.Parameters.AddWithValue("@unitTypeId", unitTypeId);
                            insertItemCmd.Parameters.AddWithValue("@unitMeasureId", unitMeasureId);
                            insertItemCmd.Parameters.AddWithValue("@minStock", minStock);
                            insertItemCmd.Parameters.AddWithValue("@maxStock", maxStock);
                            insertItemCmd.Parameters.AddWithValue("@reorderPoint", reorderPoint);
                            insertItemCmd.Parameters.AddWithValue("@leadTime", leadTime);
                            insertItemCmd.Parameters.AddWithValue("@turnOver", turnOver);
                            insertItemCmd.Parameters.AddWithValue("@enableLowStockAlert", enableLowStockAlert);

                            insertedItemId = Convert.ToInt64(insertItemCmd.ExecuteScalar());
                        }

                        string insertBatchQuery = @"
                        INSERT INTO inventory_batches (
                            item_id, batch_number, purchase_date, expiration_date,
                            initial_quantity, current_quantity, unit_cost,
                            supplier_id, is_active
                        )
                        VALUES (
                            @itemId, @batchNumber, @purchaseDate, @expirationDate,
                            @quantity, @quantity, @unitCost,
                            @supplierId, 1
                        );";

                        using (var insertBatchCmd = new MySqlCommand(insertBatchQuery, connection, transaction))
                        {
                            insertBatchCmd.Parameters.AddWithValue("@itemId", insertedItemId);
                            insertBatchCmd.Parameters.AddWithValue("@batchNumber", batchNumber);
                            insertBatchCmd.Parameters.AddWithValue("@purchaseDate", purchaseDate);
                            insertBatchCmd.Parameters.AddWithValue("@expirationDate", expirationDate);
                            insertBatchCmd.Parameters.AddWithValue("@quantity", batchQuantity);
                            insertBatchCmd.Parameters.AddWithValue("@unitCost", unitCost);
                            insertBatchCmd.Parameters.AddWithValue("@supplierId", string.IsNullOrWhiteSpace(supplierId) ? DBNull.Value : supplierId);

                            insertBatchCmd.ExecuteNonQuery();
                        }

                        transaction.Commit();

                        return new Packet
                        {
                            Type = PacketType.CreateInventoryItemResponse,
                            Success = true,
                            Message = "Inventory item and batch created successfully",
                            Data = new Dictionary<string, string>
                            {
                                { "success", "true" },
                                { "message", "Inventory item and batch created successfully" }
                            }
                        };
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Logger.Write("INVENTORY", $"Transaction failed: {ex.Message}");

                        return new Packet
                        {
                            Type = PacketType.CreateInventoryItemResponse,
                            Success = false,
                            Message = "Error creating inventory item",
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

        public Packet GetInventoryItems(Packet request)
        {
            try
            {
                string query = @"
                    SELECT 
                        i.item_id,
                        i.item_name,
                        i.description,
                        i.min_stock_level,
                        i.max_stock_level,
                        i.reorder_point,
                        i.lead_time_days,
                        i.target_turnover_days,
                        i.enable_low_stock_alert,

                        c.category_name,
                        sc.subcategory_name,
                        ut.unit_name AS unit_type_name,
                        um.name AS unit_measure_name,
                        um.symbol AS unit_measure_symbol,
    
                        b.batch_id,
                        b.batch_number,
                        b.purchase_date,
                        b.expiration_date,
                        b.initial_quantity,
                        b.current_quantity,
                        b.unit_cost,
                        b.supplier_id,
                        s.supplier_name,
                        b.status,
                        b.is_active

                    FROM inventory_items i
                    LEFT JOIN inventory_categories c ON i.category_id = c.category_id
                    LEFT JOIN inventory_subcategories sc ON i.subcategory_id = sc.subcategory_id
                    LEFT JOIN unittypes ut ON i.unit_type_id = ut.type_id
                    LEFT JOIN unitmeasures um ON i.unit_measure_id = um.measure_id
                    LEFT JOIN inventory_batches b ON i.item_id = b.item_id
                    LEFT JOIN suppliers s ON b.supplier_id = s.supplier_id;
                ";

                using (var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString))
                {
                    connection.Open();

                    var itemDict = new Dictionary<int, InventoryItem>();

                    using (var command = new MySqlCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int itemId = reader.GetInt32("item_id");

                            if (!itemDict.TryGetValue(itemId, out var item))
                            {
                                item = new InventoryItem
                                {
                                    ItemId = itemId,
                                    ItemName = reader.GetString("item_name"),
                                    Description = reader.GetString("description"),
                                    MinStockLevel = reader.GetInt32("min_stock_level"),
                                    MaxStockLevel = reader.GetInt32("max_stock_level"),
                                    ReorderPoint = reader.GetInt32("reorder_point"),
                                    LeadTimeDays = reader.GetInt32("lead_time_days"),
                                    TargetTurnoverDays = reader.GetInt32("target_turnover_days"),
                                    EnableLowStockAlert = reader.GetBoolean("enable_low_stock_alert"),

                                    CategoryName = reader.GetString("category_name"),
                                    SubcategoryName = reader.GetString("subcategory_name"),
                                    UnitTypeName = reader.GetString("unit_type_name"),
                                    UnitMeasureName = reader.GetString("unit_measure_name"),
                                    UnitMeasureSymbol = reader.GetString("unit_measure_symbol"),
                                    Batches = new List<InventoryBatch>()
                                };

                                itemDict[itemId] = item;
                            }

                            if (!reader.IsDBNull(reader.GetOrdinal("batch_number")))
                            {
                                var batch = new InventoryBatch
                                {
                                    BatchId = reader.GetInt32("batch_id"),
                                    BatchNumber = reader.GetString("batch_number"),
                                    PurchaseDate = reader.IsDBNull(reader.GetOrdinal("purchase_date")) ? null : reader.GetDateTime("purchase_date"),
                                    ExpirationDate = reader.IsDBNull(reader.GetOrdinal("expiration_date")) ? null : reader.GetDateTime("expiration_date"),
                                    InitialQuantity = reader.IsDBNull(reader.GetOrdinal("initial_quantity")) ? 0 : reader.GetInt32("initial_quantity"),
                                    CurrentQuantity = reader.IsDBNull(reader.GetOrdinal("current_quantity")) ? 0 : reader.GetInt32("current_quantity"),
                                    UnitCost = reader.IsDBNull(reader.GetOrdinal("unit_cost")) ? 0 : reader.GetDecimal("unit_cost"),
                                    SupplierId = reader.IsDBNull(reader.GetOrdinal("supplier_id")) ? 0 : reader.GetInt32("supplier_id"),
                                    SupplierName = reader.IsDBNull(reader.GetOrdinal("supplier_name")) ? "" : reader.GetString("supplier_name"),
                                    Status = reader.IsDBNull(reader.GetOrdinal("status")) ? "" : reader.GetString("status"),
                                    IsActive = reader.IsDBNull(reader.GetOrdinal("is_active")) ? 0 : reader.GetInt32("is_active")
                                };

                                item.Batches.Add(batch);
                            }
                        }

                        var itemList = itemDict.Values.ToList();

                        Logger.Write("GET INVENTORY", $"Retrieved {itemList.Count} items from inventory");

                        return new Packet
                        {
                            Type = PacketType.GetInventoryItemResponse,
                            Success = true,
                            Message = "Inventory items retrieved successfully",
                            Data = new Dictionary<string, string>
                            {
                                { "success", "true" },
                                { "message", "Inventory items retrieved successfully" },
                                { "inventoryitems", System.Text.Json.JsonSerializer.Serialize(itemList) }
                            }
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write("INVENTORY REQUEST", $"Inventory request error: {ex.Message}");
                return new Packet
                {
                    Type = PacketType.GetInventoryItemResponse,
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

        public Packet CreateBatch(Packet packet)
        {
            var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString);

            try
            {
                connection.Open();

                int itemId = int.Parse(packet.Data["itemId"]);
                string batchNumber = packet.Data["batchNumber"];
                string purchaseDate = packet.Data["purchaseDate"];
                string expirationDate = packet.Data["expirationDate"];
                decimal quantity = decimal.Parse(packet.Data["quantity"]);
                decimal unitCost = decimal.Parse(packet.Data["unitCost"]);
                int? supplierId = string.IsNullOrEmpty(packet.Data["supplierId"]) ? (int?)null : int.Parse(packet.Data["supplierId"]);
                bool isActive = bool.Parse(packet.Data["isActive"]);

                string checkQuery = "SELECT COUNT(*) FROM inventory_batches WHERE batch_number = @batchNumber";
                using (var checkCommand = new MySqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@batchNumber", batchNumber.Trim());
                    int count = Convert.ToInt32(checkCommand.ExecuteScalar());

                    if (count > 0)
                    {
                        return new Packet
                        {
                            Type = PacketType.CreateBatchResponse,
                            Success = false,
                            Message = "Batch number already exists",
                            Data = new Dictionary<string, string>
                            {
                                { "success", "false" },
                                { "message", "Batch number already exists" }
                            }
                        };
                    }
                }

                string insertQuery = @"
                INSERT INTO inventory_batches (item_id, batch_number, purchase_date, expiration_date, initial_quantity, current_quantity, unit_cost, supplier_id, is_active)
                VALUES (@itemId, @batchNumber, @purchaseDate, @expirationDate, @quantity, @currentQuantity, @unitCost, @supplierId, @isActive)";

                using (var command = new MySqlCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@itemId", itemId);
                    command.Parameters.AddWithValue("@batchNumber", batchNumber);
                    command.Parameters.AddWithValue("@purchaseDate", purchaseDate);
                    command.Parameters.AddWithValue("@expirationDate", expirationDate);
                    command.Parameters.AddWithValue("@quantity", quantity);
                    command.Parameters.AddWithValue("@currentQuantity", quantity);
                    command.Parameters.AddWithValue("@unitCost", unitCost);
                    command.Parameters.AddWithValue("@supplierId", (object?)supplierId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@isActive", isActive);

                    command.ExecuteNonQuery();
                }

                return new Packet
                {
                    Type = PacketType.CreateBatchResponse,
                    Success = true,
                    Message = "Batch created successfully",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "true" },
                        { "message", $"Batch '{batchNumber}' created successfully" }
                    }
                };
            }
            catch (Exception ex)
            {
                Logger.Write("BATCH", $"Error creating batch: {ex.Message}");

                return new Packet
                {
                    Type = PacketType.CreateBatchResponse,
                    Success = false,
                    Message = "Error creating batch",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", "Internal server error" }
                    }
                };
            }
        }

        public Packet UpdateBatch(Packet packet)
        {
            var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString);

            try
            {
                connection.Open();

                string batchId = packet.Data["batchId"];
                string purchaseDate = packet.Data["purchaseDate"];
                string expirationDate = packet.Data["expirationDate"];
                decimal initialQuantity = decimal.Parse(packet.Data["initialQuantity"]);
                decimal currentQuantity = decimal.Parse(packet.Data["currentQuantity"]);
                decimal unitCost = decimal.Parse(packet.Data["unitCost"]);
                int? supplierId = string.IsNullOrEmpty(packet.Data["supplierId"]) ? (int?)null : int.Parse(packet.Data["supplierId"]);
                bool isActive = bool.Parse(packet.Data["isActive"]);

                string checkQuery = "SELECT COUNT(*) FROM inventory_batches WHERE batch_id = @batchId";
                using (var checkCommand = new MySqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@batchId", batchId.Trim());
                    int count = Convert.ToInt32(checkCommand.ExecuteScalar());

                    if (count == 0)
                    {
                        return new Packet
                        {
                            Type = PacketType.UpdateBatchResponse,
                            Success = false,
                            Message = "Batch not found",
                            Data = new Dictionary<string, string>
                            {
                                { "success", "false" },
                                { "message", "Batch not found" }
                            }
                        };
                    }
                }

                string updateQuery = @"
                UPDATE inventory_batches
                SET purchase_date = @purchaseDate,
                    expiration_date = @expirationDate, 
                    initial_quantity = @initialQuantity, 
                    current_quantity = @currentQuantity, 
                    unit_cost = @unitCost, 
                    supplier_id = @supplierId, 
                    is_active = @isActive
                WHERE batch_id = @batchId";

                using (var updateCommand = new MySqlCommand(updateQuery, connection))
                {
                    updateCommand.Parameters.AddWithValue("@batchId", batchId);
                    updateCommand.Parameters.AddWithValue("@purchaseDate", purchaseDate);
                    updateCommand.Parameters.AddWithValue("@expirationDate", expirationDate);
                    updateCommand.Parameters.AddWithValue("@initialQuantity", initialQuantity);
                    updateCommand.Parameters.AddWithValue("@currentQuantity", currentQuantity);
                    updateCommand.Parameters.AddWithValue("@unitCost", unitCost);
                    updateCommand.Parameters.AddWithValue("@supplierId", supplierId > 0 ? (object)supplierId : DBNull.Value);
                    updateCommand.Parameters.AddWithValue("@isActive", isActive);

                    updateCommand.ExecuteNonQuery();
                }

                return new Packet
                {
                    Type = PacketType.UpdateBatchResponse,
                    Success = true,
                    Message = "Batch updated successfully",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "true" },
                        { "message", $"Batch '{batchId}' updated successfully" }
                    }
                };
            }
            catch (Exception ex)
            {
                Logger.Write("BATCH", $"Error updating batch: {ex.Message}");

                return new Packet
                {
                    Type = PacketType.UpdateBatchResponse,
                    Success = false,
                    Message = "Error updating batch",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", "Internal server error" }
                    }
                };
            }
        }

        public Packet GetBatch(Packet request)
        {
            try
            {
                if (!request.Data.ContainsKey("itemId"))
                {
                    return new Packet
                    {
                        Type = PacketType.GetBatchResponse,
                        Success = false,
                        Message = "Item ID is required",
                        Data = new Dictionary<string, string>
                        {
                            { "success", "false" },
                            { "message", "Item ID is required" }
                        }
                    };
                }

                string itemId = request.Data["itemId"];

                string query = @"
                    SELECT 
                        b.batch_id,
                        b.item_id,
                        b.batch_number,
                        b.purchase_date,
                        b.expiration_date,
                        b.initial_quantity,
                        b.current_quantity,
                        b.unit_cost,
                        b.supplier_id,
                        IFNULL(s.supplier_name, '') AS supplier_name,
                        b.status,
                        b.is_active
                    FROM inventory_batches b
                    LEFT JOIN suppliers s ON b.supplier_id = s.supplier_id AND s.is_active = 1
                    WHERE b.item_id = @itemId;
                ";

                using (var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString))
                {
                    connection.Open();

                    var batches = new List<Dictionary<string, object>>();

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@itemId", itemId);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var batch = new Dictionary<string, object>();

                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    var columnName = reader.GetName(i);
                                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                                    batch[columnName] = value!;
                                }

                                batches.Add(batch);
                            }
                        }
                    }

                    Logger.Write("GET BATCH SERVER", $"Found {batches.Count} batch(es) for item ID {itemId}");

                    return new Packet
                    {
                        Type = PacketType.GetBatchResponse,
                        Success = true,
                        Message = "Batch(es) retrieved successfully",
                        Data = new Dictionary<string, string>
                        {
                            { "success", "true" },
                            { "message", "Batch(es) retrieved successfully" },
                            { "batches", System.Text.Json.JsonSerializer.Serialize(batches) }
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.Write("GET BATCH SERVER ERROR", $"Error: {ex.Message}");

                return new Packet
                {
                    Type = PacketType.GetBatchResponse,
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
    }
}
