using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.Models
{
    public class TransactionItem
    {
        public Product? Product { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string? Notes { get; set; }
        public decimal TotalPrice => Quantity * Price;
    }
}
