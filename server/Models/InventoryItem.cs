using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.Models
{
    public class InventoryItem
    {
        public int ItemId { get; set; }
        public string? ItemName { get; set; }
        public string? Description { get; set; }
        public int MinStockLevel { get; set; }
        public int MaxStockLevel { get; set; }
        public int ReorderPoint { get; set; }
        public int LeadTimeDays { get; set; }
        public int TargetTurnoverDays { get; set; }
        public bool EnableLowStockAlert { get; set; }

        public string? CategoryName { get; set; }
        public string? SubcategoryName { get; set; }
        public string? UnitTypeName { get; set; }
        public string? UnitMeasureName { get; set; }

        public List<InventoryBatch> Batches { get; set; } = new List<InventoryBatch>();
    }
}
