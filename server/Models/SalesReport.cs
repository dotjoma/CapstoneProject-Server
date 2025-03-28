using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.Models
{
    public class SalesReport
    {
        public int Id { get; set; }
        public string? TransactionNo { get; set; }
        public int ItemId { get; set; }
        public int CashierId { get; set; }
        public string? CashierFName { get; set; }
        public string? CashierLName { get; set; }
        public int Quantity { get; set; }
        public decimal Discount { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Notes { get; set; }
        public string? OrderType { get; set; }
        public DateTime? OrderTime { get; set; }
        public DateTime? OrderDate { get; set; }
    }
}
