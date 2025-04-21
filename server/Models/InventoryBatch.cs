using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.Models
{
    public class InventoryBatch
    {
        public int BatchId { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public int InitialQuantity { get; set; }
        public int CurrentQuantity { get; set; }
        public decimal UnitCost { get; set; }
        public string? SupplierName { get; set; }
        public int IsActive { get; set; }
    }
}
