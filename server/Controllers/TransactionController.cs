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

                            transaction.Commit();

                            Logger.Write("TRANSACTION", $"Generated transaction: {transNumber}, Order: {orderNumber}");

                            return new Packet
                            {
                                Type = PacketType.GenerateTransactionNumbers,
                                Success = true,
                                Message = "Transaction numbers generated and saved successfully",
                                Data = new Dictionary<string, string>
                                {
                                    { "success", "true" },
                                    { "message", "Transaction numbers generated successfully" },
                                    { "transNumber", transNumber },
                                    { "orderNumber", orderNumber }
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
                                        { "message", "Failed to generate unique transaction numbers" }
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

        public bool ProcessTransaction(int transId, decimal totalAmount, string paymentMethod,
    List<TransactionItem> transactionItems, Payment payment)
        {
            if (!ValidateTransactionInput(transId, totalAmount, paymentMethod, transactionItems, payment))
            {
                Logger.Write("TRANSACTION", "Invalid input parameters");
                return false;
            }

            using (var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString))
            {
                try
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Verify transaction exists and is unpaid
                            if (!VerifyTransactionStatus(transId, connection, transaction))
                            {
                                transaction.Rollback();
                                return false;
                            }

                            // Update Transactions table
                            string updateTransactionQuery = @"
                                UPDATE transactions 
                                SET total_amount = @totalAmount, 
                                    status = 'paid', 
                                    payment_method = @paymentMethod,
                                    updated_at = CURRENT_TIMESTAMP 
                                WHERE trans_id = @transId";

                            using (var cmd = new MySqlCommand(updateTransactionQuery, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@totalAmount", totalAmount);
                                cmd.Parameters.AddWithValue("@paymentMethod", paymentMethod);
                                cmd.Parameters.AddWithValue("@transId", transId);
                                cmd.ExecuteNonQuery();
                            }

                            // Insert Order Details
                            string insertOrderDetailsQuery = @"
                                INSERT INTO orderdetails 
                                (trans_id, item_id, quantity, price, total_price, notes) 
                                VALUES 
                                (@transId, @itemId, @quantity, @price, @totalPrice, @notes)";

                            foreach (var item in transactionItems)
                            {
                                using (var cmd = new MySqlCommand(insertOrderDetailsQuery, connection, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@transId", transId);
                                    cmd.Parameters.AddWithValue("@itemId", item.Product?.productId);
                                    cmd.Parameters.AddWithValue("@quantity", item.Quantity);
                                    cmd.Parameters.AddWithValue("@price", item.Price);
                                    cmd.Parameters.AddWithValue("@totalPrice", item.TotalPrice);
                                    cmd.Parameters.AddWithValue("@notes", item.Notes ?? "");
                                    cmd.ExecuteNonQuery();
                                }
                            }

                            // Insert Payment Record
                            string insertPaymentQuery = @"
                                INSERT INTO payments 
                                (trans_id, amount_paid, payment_method, reference_number, 
                                    payment_time, change_amount, notes) 
                                VALUES 
                                (@transId, @amountPaid, @paymentMethod, @referenceNumber, 
                                    CURRENT_TIMESTAMP, @changeAmount, @notes)";

                            using (var cmd = new MySqlCommand(insertPaymentQuery, connection, transaction))
                            {
                                cmd.Parameters.AddWithValue("@transId", transId);
                                cmd.Parameters.AddWithValue("@amountPaid", payment.AmountPaid);
                                cmd.Parameters.AddWithValue("@paymentMethod", payment.PaymentMethod);
                                cmd.Parameters.AddWithValue("@referenceNumber",
                                    payment.ReferenceNumber ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@changeAmount", payment.ChangeAmount);
                                cmd.Parameters.AddWithValue("@notes", payment.Notes ?? "");
                                cmd.ExecuteNonQuery();
                            }

                            transaction.Commit();
                            Logger.Write("TRANSACTION", $"Successfully processed transaction {transId}");
                            return true;
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            Logger.Write("TRANSACTION", $"Error processing transaction {transId}: {ex.Message}");
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Write("TRANSACTION", $"Database connection error: {ex.Message}");
                    return false;
                }
            }
        }

        private bool ValidateTransactionInput(int transId, decimal totalAmount, string paymentMethod,
        List<TransactionItem> transactionItems, Payment payment)
        {
            if (transId <= 0)
            {
                Logger.Write("VALIDATION", "Invalid transaction ID");
                return false;
            }

            if (totalAmount <= 0)
            {
                Logger.Write("VALIDATION", "Invalid total amount");
                return false;
            }

            if (string.IsNullOrEmpty(paymentMethod))
            {
                Logger.Write("VALIDATION", "Payment method is required");
                return false;
            }

            if (transactionItems == null || !transactionItems.Any())
            {
                Logger.Write("VALIDATION", "Transaction items are required");
                return false;
            }

            if (payment == null)
            {
                Logger.Write("VALIDATION", "Payment details are required");
                return false;
            }

            // Validate transaction items
            foreach (var item in transactionItems)
            {
                if (item.Product?.productId <= 0 || item.Quantity <= 0 || item.Price < 0)
                {
                    Logger.Write("VALIDATION",
                        $"Invalid item details - ProductId: {item.Product?.productId}, " +
                        $"Quantity: {item.Quantity}, Price: {item.Price}");
                    return false;
                }
            }

            // Validate payment
            if (payment.AmountPaid <= 0)
            {
                Logger.Write("VALIDATION", "Invalid payment amount");
                return false;
            }

            return true;
        }

        private bool VerifyTransactionStatus(int transId, MySqlConnection connection, MySqlTransaction transaction)
        {
            string verifyQuery = "SELECT status FROM transactions WHERE trans_id = @transId FOR UPDATE";

            using (var cmd = new MySqlCommand(verifyQuery, connection, transaction))
            {
                cmd.Parameters.AddWithValue("@transId", transId);
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
