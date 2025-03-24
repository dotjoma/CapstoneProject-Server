using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.Models
{
    public class OrderProcessing
    {
        public string? TransNo { get; set; }
        public int ProductId { get; set; }
        public int CashierId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice { get; set; }
        public string? Notes { get; set; }
        public string? OrderType { get; set; }
    }
}
