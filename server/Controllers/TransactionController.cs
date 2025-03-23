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
            // Deserialize the transaction data from the request
            var transactionData = JsonConvert.DeserializeObject<Dictionary<string, string>>(request.Data["transaction"]);

            // Validate the transaction data
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

            // Extract transaction details
            string transId = transactionData["transId"];
            string paymentMethod = transactionData["paymentMethod"];

            // Connect to the database
            using (var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString))
            {
                connection.Open();

                // Begin a database transaction
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Verify the transaction status
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

                        // Update the transaction status in the database
                        var updateTransactionQuery = @"
                            UPDATE transactions 
                            SET status = 'paid', 
                                paymentMethod = @PaymentMethod,
                                updated_at = CURRENT_TIMESTAMP 
                            WHERE transId = @TransId"; // Updated to use transId

                        connection.Execute(updateTransactionQuery,
                            new { PaymentMethod = paymentMethod, TransId = transId },
                            transaction);

                        // Commit the transaction
                        transaction.Commit();
                        Logger.Write("TRANSACTION", $"Successfully processed transaction {transId}");

                        // Return a success response
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
                        // Rollback the transaction in case of an error
                        transaction.Rollback();
                        Logger.Write("TRANSACTION", $"Error processing transaction {transId}: {ex.Message}");

                        // Return an error response
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
