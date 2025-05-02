using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.Models
{
    public class ProductIngredient
    {
        public int InventoryItemId { get; set; }
        public decimal Quantity { get; set; }
        public string? MeasureSymbol { get; set; }
    }
}
