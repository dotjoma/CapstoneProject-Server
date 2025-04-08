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
using Dapper;
using Org.BouncyCastle.Asn1.Ocsp;
using Newtonsoft.Json;

namespace server.Controllers
{
    public class TransactionController
    {
        public Packet GenerateTransactionNumbers(Packet packet, int retryCount = 0)
        {
            const int MAX_RETRIES = 3;

            try
            {
                using (var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString))
                {
                    connection.Open();

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            string today = DateTime.Now.ToString("yyyyMMdd");
                            int nextTransId = GetNextTransactionId(connection, transaction);
                            int nextOrderNumber = GetNextOrderNumber(connection, transaction);

                            string transNumber = $"{today}{nextTransId:D4}";
                            string orderNumber = $"{nextOrderNumber:D3}";

                            // Insert the generated transaction and order numbers into the database
                            string insertQuery = @"
                            INSERT INTO transactions
                            (transNumber, orderNumber, transDate) 
                            VALUES 
                            (@transNumber, @orderNumber, CURDATE())";

                            using (var command = new MySqlCommand(insertQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@transNumber", transNumber);
                                command.Parameters.AddWithValue("@orderNumber", orderNumber);
                                command.ExecuteNonQuery();
                            }

                            string selectQuery = "SELECT transID, transNumber, orderNumber FROM transactions WHERE transNumber = @transNumber";
                            string? fetchedTransId = string.Empty;
                            string? fetchedTransNumber = string.Empty;
                            string? fetchedOrderNumber = string.Empty;

                            using (var command = new MySqlCommand(selectQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@transNumber", transNumber);
                                using (var reader = command.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        fetchedTransId = reader["transID"] == DBNull.Value ? null : reader["transID"].ToString();
                                        fetchedTransNumber = reader["transNumber"] == DBNull.Value ? null : reader["transNumber"].ToString();
                                        fetchedOrderNumber = reader["orderNumber"] == DBNull.Value ? null : reader["orderNumber"].ToString();
                                    }
                                }
                            }

                            // Commit the transaction
                            transaction.Commit();

                            Logger.Write("TRANSACTION", $"Generated transaction: {fetchedTransNumber}, Order: {fetchedOrderNumber}, TransID: {fetchedTransId}");

                            return new Packet
                            {
                                Type = PacketType.GenerateTransactionNumbers,
                                Success = true,
                                Message = "Transaction numbers generated and saved successfully",
                                Data = new Dictionary<string, string>
                                {
                                    { "success", "true" },
                                    { "message", "Transaction numbers generated successfully" },
                                    { "transID", fetchedTransId ?? "" },
                                    { "transNumber", fetchedTransNumber ?? "" },
                                    { "orderNumber", fetchedOrderNumber ?? "" }
                                }
                            };
                        }
                        catch (MySqlException sqlEx) when (sqlEx.Number == 1062)
                        {
                            transaction.Rollback();
                            if (retryCount < MAX_RETRIES)
                            {
                                Logger.Write("TRANSACTION", $"Duplicate transaction detected! Retry attempt {retryCount + 1}...");
                                return GenerateTransactionNumbers(packet, retryCount + 1);
                            }
                            else
                            {
                                Logger.Write("TRANSACTION", "Max retry attempts reached for duplicate transaction");
                                return new Packet
                                {
                                    Type = PacketType.GenerateTransactionNumbers,
                                    Success = false,
                                    Message = "Failed to generate unique transaction numbers after multiple attempts",
                                    Data = new Dictionary<string, string>
                                    {
                                        { "success", "false" },
                                        { "message", "Failed to generate unique transaction numbers" },
                                        { "retryCount", retryCount.ToString() }
                                    }
                                };
                            }
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            Logger.Write("TRANSACTION", $"Error generating transaction numbers: {ex.Message}");
                            return new Packet
                            {
                                Type = PacketType.GenerateTransactionNumbers,
                                Success = false,
                                Message = "Error generating transaction numbers",
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
            catch (Exception ex)
            {
                Logger.Write("TRANSACTION", $"Critical Error: {ex.Message}");
                return new Packet
                {
                    Type = PacketType.GenerateTransactionNumbers,
                    Success = false,
                    Message = "Critical error",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", "Critical error occurred" }
                    }
                };
            }
        }

        public Packet ProcessTransaction(Packet request)
        {
            var transactionData = JsonConvert.DeserializeObject<Dictionary<string, string>>(request.Data["transaction"]);
            var orderData = JsonConvert.DeserializeObject<List<OrderProcessing>>(request.Data["order"]);
            var paymentData = JsonConvert.DeserializeObject<Dictionary<string, string>>(request.Data["payment"]);

            if (transactionData == null || !transactionData.ContainsKey("transId"))
            {
                return new Packet
                {
                    Type = PacketType.ProcessTransactionResponse,
                    Success = false,
                    Message = "Missing transId in transaction data.",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", "Missing transId in transaction data." }
                    }
                };
            }

            if (paymentData == null || !paymentData.ContainsKey("transId"))
            {
                return new Packet
                {
                    Type = PacketType.ProcessTransactionResponse,
                    Success = false,
                    Message = "Missing transId in payment data.",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", "Missing transId in payment data." }
                    }
                };
            }

            string transId = transactionData["transId"];
            string totalAmount = transactionData["totalAmount"];
            string status = transactionData["status"];
            string paymentMethod = transactionData["paymentMethod"];

            string amountPaid = paymentData["amountPaid"];
            string referenceNo = paymentData["referenceNo"];
            string changeAmount = paymentData["changeAmount"];

            using (var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        if (!VerifyTransactionStatus(transId, connection, transaction))
                        {
                            transaction.Rollback();
                            return new Packet
                            {
                                Type = PacketType.ProcessTransactionResponse,
                                Success = false,
                                Message = "Transaction not found or already paid",
                                Data = new Dictionary<string, string>
                                {
                                    { "success", "false" },
                                    { "message", "Transaction not found or already paid" }
                                }
                            };
                        }

                        if (orderData != null && orderData.Any())
                        {
                            var validOrders = new List<OrderProcessing>();
                            var invalidOrders = new List<(OrderProcessing order, string reason)>();

                            foreach (var order in orderData)
                            {
                                if (ValidateOrder(order, out var errorMessage))
                                {
                                    validOrders.Add(order);
                                }
                                else
                                {
                                    invalidOrders.Add((order, errorMessage));
                                }
                            }

                            if (invalidOrders.Any())
                            {
                                Logger.Write("VALIDATION",
                                    $"{invalidOrders.Count} invalid orders detected. Reasons: {string.Join(", ", invalidOrders.Select(x => x.reason))}");
                            }

                            if (validOrders.Any())
                            {
                                try
                                {
                                    var insertOrderQuery = @"
                                    INSERT INTO orderdetails 
                                    (trans_no, item_id, cashier_id, quantity, discount, price, total_price, notes, order_type, order_time, order_date)
                                    VALUES 
                                    (@TransNo, @ProductId, @CashierId, @Quantity, @Discount, @Price, @TotalPrice, @Notes, @OrderType, CURTIME(), CURDATE())";

                                    connection.Execute(insertOrderQuery,
                                        validOrders.Select(order => new
                                        {
                                            TransNo = order.TransNo,
                                            ProductId = order.ProductId,
                                            CashierId = order.CashierId,
                                            Quantity = order.Quantity,
                                            Discount = order.Discount,
                                            Price = order.Price,
                                            TotalPrice = order.TotalPrice,
                                            Notes = order.Notes,
                                            OrderType = order.OrderType
                                        }),
                                        transaction);

                                    Logger.Write("ORDER", $"Successfully inserted {validOrders.Count} valid orders");
                                }
                                catch (Exception ex)
                                {
                                    Logger.Write("ORDER_ERROR", $"Failed to insert valid orders: {ex.Message}");
                                    throw;
                                }
                            }
                            else
                            {
                                Logger.Write("ORDER", "No valid orders to process");
                            }
                        }
                        else
                        {
                            Logger.Write("WARNING", "Empty order data received - no orders to process");
                        }

                        var insertPaymentQuery = @"
                        INSERT INTO payments 
                        (trans_id, amount_paid, payment_method, reference_number, payment_time, change_amount)
                        VALUES 
                        (
                            @TransId,
                            @AmountPaid,
                            @PaymentMethod,
                            CASE 
                                WHEN @PaymentMethod = 'cash'
                                    THEN CONCAT('CASH-', @TransId, '-', DATE_FORMAT(NOW(), '%Y%m%d%H%i%s'))
                                ELSE @ReferenceNumber
                            END,
                            CURTIME(), 
                            @ChangeAmount
                        )";

                        connection.Execute(insertPaymentQuery,
                            new 
                            { 
                                TransId = transId, 
                                AmountPaid = amountPaid, 
                                PaymentMethod = paymentMethod, 
                                ReferenceNumber = referenceNo,
                                ChangeAmount = changeAmount
                            },
                            transaction);

                        var updateTransactionQuery = @"
                        UPDATE transactions 
                        SET totalAmount = @TotalAmount,
                            status = @Status, 
                            paymentMethod = @PaymentMethod,
                            updated_at = CURRENT_TIMESTAMP 
                        WHERE transId = @TransId";

                        connection.Execute(updateTransactionQuery,
                            new 
                            { 
                                TotalAmount = totalAmount, 
                                Status = status, 
                                PaymentMethod = paymentMethod, 
                                TransId = transId 
                            },
                            transaction);

                        transaction.Commit();

                        Logger.Write("TRANSACTION", $"Successfully processed transaction {transId}");

                        return new Packet
                        {
                            Type = PacketType.ProcessTransactionResponse,
                            Success = true,
                            Message = $"Transaction {transId} processed successfully.",
                            Data = new Dictionary<string, string>
                            {
                                { "success", "true" },
                                { "message", $"Transaction {transId} processed successfully." }
                            }
                        };
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Logger.Write("TRANSACTION", $"Error processing transaction {transId}: {ex.Message}");

                        return new Packet
                        {
                            Type = PacketType.ProcessTransactionResponse,
                            Success = false,
                            Message = $"Error processing transaction {transId}: {ex.Message}",
                            Data = new Dictionary<string, string>
                            {
                                { "success", "false" },
                                { "message", $"Error processing transaction {transId}: {ex.Message}" }
                            }
                        };
                    }
                }
            }
        }

        private bool ValidateOrder(OrderProcessing order, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (order == null)
            {
                errorMessage = "Order is null";
                Logger.Write("VALIDATION", errorMessage);
                return false;
            }

            if (string.IsNullOrWhiteSpace(order.TransNo))
            {
                errorMessage = "Transaction number is required";
                Logger.Write("VALIDATION", $"{errorMessage} | Order: {order.ProductId}");
                return false;
            }

            if (order.ProductId <= 0)
            {
                errorMessage = $"Invalid product ID: {order.ProductId}";
                Logger.Write("VALIDATION", errorMessage);
                return false;
            }

            if (order.CashierId <= 0)
            {
                errorMessage = $"Invalid cashier ID: {order.CashierId}";
                Logger.Write("VALIDATION", errorMessage);
                return false;
            }

            if (order.Quantity <= 0)
            {
                errorMessage = $"Quantity must be positive: {order.Quantity}";
                Logger.Write("VALIDATION", errorMessage);
                return false;
            }

            if (order.Price <= 0)
            {
                errorMessage = $"Price must be positive: {order.Price}";
                Logger.Write("VALIDATION", errorMessage);
                return false;
            }

            if (order.Discount < 0)
            {
                errorMessage = $"Discount cannot be negative: {order.Discount}";
                Logger.Write("VALIDATION", errorMessage);
                return false;
            }

            if (string.IsNullOrWhiteSpace(order.OrderType))
            {
                errorMessage = "Order type is required";
                Logger.Write("VALIDATION", $"{errorMessage} | Order: {order.ProductId}");
                return false;
            }

            return true;
        }

        private bool VerifyTransactionStatus(string transId, MySqlConnection connection, MySqlTransaction transaction)
        {
            string verifyQuery = "SELECT status FROM transactions WHERE transID = @transID FOR UPDATE";

            using (var cmd = new MySqlCommand(verifyQuery, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@transID", transId);
                string? status = cmd.ExecuteScalar()?.ToString();

                if (status == null)
                {
                    Logger.Write("VALIDATION", $"Transaction {transId} not found");
                    return false;
                }

                if (status != "unpaid")
                {
                    Logger.Write("VALIDATION", $"Invalid transaction status: {status}");
                    return false;
                }

                return true;
            }
        }

        public Packet SaveTransaction(Packet packet)
        {
            var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString);
            try
            {
                connection.Open();
                string transNumber = packet.Data["transNumber"];
                string orderNumber = packet.Data["orderNumber"];

                using (var cmd = new MySqlCommand(
                    "INSERT INTO transactions (transNumber, orderNumber, transDate) VALUES (@transNumber, @orderNumber, CURDATE())",
                    connection))
                {
                    cmd.Parameters.AddWithValue("@transNumber", transNumber);
                    cmd.Parameters.AddWithValue("@orderNumber", orderNumber);
                    cmd.ExecuteNonQuery();

                    return new Packet
                    {
                        Type = PacketType.SaveTransactionResponse,
                        Success = true,
                        Message = "Transaction saved successfully",
                        Data = new Dictionary<string, string>
                        {
                            { "success", "true" },
                            { "message", "Transaction saved successfully" }
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                Logger.Write("TRANSACTION", $"Error saving transaction: {ex.Message}");
                return new Packet
                {
                    Type = PacketType.SaveTransactionResponse,
                    Success = false,
                    Message = "Error saving transaction",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", "Internal server error" }
                    }
                };
            }
        }

        public Packet RemoveTransaction(Packet packet)
        {
            var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString);
            try
            {
                connection.Open();
                string transNumber = packet.Data["transNumber"];

                using (var cmd = new MySqlCommand(
                    "DELETE FROM transactions WHERE transNumber = @transNumber", connection))
                {
                    cmd.Parameters.AddWithValue("@transNumber", transNumber);
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        Logger.Write("TRANSACTION", $"Transaction {transNumber} removed successfully.");
                        return new Packet
                        {
                            Type = PacketType.RemoveTransactionResponse,
                            Success = true,
                            Message = "Transaction removed successfully",
                            Data = new Dictionary<string, string>
                            {
                                { "success", "true" },
                                { "message", "Transaction removed successfully" }
                            }
                        };
                    }
                    else
                    {
                        Logger.Write("TRANSACTION", $"Transaction {transNumber} not found.");
                        return new Packet
                        {
                            Type = PacketType.RemoveTransactionResponse,
                            Success = false,
                            Message = "Transaction not found",
                            Data = new Dictionary<string, string>
                            {
                                { "success", "false" },
                                { "message", "Transaction not found" }
                            }
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Write("TRANSACTION", $"Error removing transaction: {ex.Message}");
                return new Packet
                {
                    Type = PacketType.RemoveTransactionResponse,
                    Success = false,
                    Message = "Error removing transaction",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", "Internal server error" }
                    }
                };
            }
        }

        public int GetNextTransactionId(MySqlConnection connection, MySqlTransaction transaction)
        {
            using (var cmd = new MySqlCommand("SELECT MAX(transID) FROM transactions", connection, transaction))
            {
                object result = cmd.ExecuteScalar();
                return result != DBNull.Value ? Convert.ToInt32(result) + 1 : 1;
            }
        }

        public int GetNextOrderNumber(MySqlConnection connection, MySqlTransaction transaction)
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            using (var cmd = new MySqlCommand(
                "SELECT MAX(orderNumber) FROM transactions WHERE DATE(transDate) = @date",
                connection,
                transaction))
            {
                cmd.Parameters.AddWithValue("@date", today);
                object result = cmd.ExecuteScalar();
                return result != DBNull.Value ? Convert.ToInt32(result) + 1 : 1;
            }
        }
    }
}
