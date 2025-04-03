using MySql.Data.MySqlClient;
using server.Core.Network;
using server.Database;
using server.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using client.Helpers;
using System.Threading.Tasks;
using K4os.Compression.LZ4;
using System.Data;
using System.Text.Json;
using Org.BouncyCastle.Asn1.Ocsp;

namespace server.Controllers
{
    public class SalesReportController
    {
        public Packet GetSalesReport(Packet packet)
        {
            var connection = new MySqlConnection(DatabaseManager.Instance.ConnectionString);
            var salesReports = new List<SalesReport>();

            try
            {
                connection.Open();

                var query = @"
                SELECT 
                    od.id, od.trans_no, od.item_id, od.cashier_id, 
                    od.quantity, od.discount, od.price, od.total_price, 
                    od.notes, od.order_type, od.order_time, od.order_date,
                    u.fname AS cashier_fname, u.lname AS cashier_lname
                FROM orderdetails od
                LEFT JOIN users u ON od.cashier_id = u.id";

                using (var command = new MySqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        salesReports.Add(new SalesReport
                        {
                            Id = reader.GetInt32("id"),
                            TransactionNo = reader.GetInt64("trans_no").ToString(),
                            ItemId = reader.GetInt32("item_id"),
                            CashierId = reader.GetInt32("cashier_id"),
                            Quantity = reader.GetInt32("quantity"),
                            Discount = reader.GetDecimal("discount"),
                            Price = reader.GetDecimal("price"),
                            TotalPrice = reader.GetDecimal("total_price"),
                            Notes = reader.IsDBNull("notes") ? "" : reader.GetString("notes"),
                            OrderType = reader.GetString("order_type"),

                            OrderTime = reader.IsDBNull("order_time")
                                ? null
                                : (DateTime?)DateTime.Today.Add(reader.GetTimeSpan("order_time")),
                            OrderDate = reader.IsDBNull("order_date")
                                ? null
                                : (DateTime?)reader.GetDateTime("order_date"),

                            CashierFName = reader.IsDBNull("cashier_fname")
                                ? "Unknown"
                                : reader.GetString("cashier_fname"),
                            CashierLName = reader.IsDBNull("cashier_lname")
                                ? ""
                                : reader.GetString("cashier_lname")
                        });
                    }
                }

                Logger.Write("SALESREPORT", salesReports.Count > 0
                    ? $"Retrieved {salesReports.Count} sales reports"
                    : "No sales reports found");

                return new Packet
                {
                    Type = PacketType.GetSalesReportResponse,
                    Success = true,
                    Message = salesReports.Count > 0
                        ? "Sales reports retrieved successfully"
                        : "No sales reports found",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "true" },
                        { "message", salesReports.Count > 0
                            ? "Sales reports retrieved successfully"
                            : "No sales reports found" },
                        { "salesreports", JsonSerializer.Serialize(salesReports) }
                    }
                };
            }
            catch (Exception ex)
            {
                Logger.Write("SALESREPORT", $"Error retrieving salesreports: {ex.Message}");
                return new Packet
                {
                    Type = PacketType.GetSalesReportResponse,
                    Success = false,
                    Message = "Error retrieving sales reports",
                    Data = new Dictionary<string, string>
                    {
                        { "success", "false" },
                        { "message", $"Error: {ex.Message}" },
                        { "salesreports", "[]" }
                    }
                };
            }
        }
    }
}
