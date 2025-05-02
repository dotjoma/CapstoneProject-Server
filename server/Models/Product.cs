using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.Models
{
    public class Product
    {
        public int productId { get; set; }

        public int? categoryId { get; set; }

        public int? subcategoryId { get; set; }

        public string? productName { get; set; }

        public int? unitId { get; set; }

        public decimal? productPrice { get; set; }

        public string? productImage { get; set; }

        public int? isVatable { get; set; }

        public int isActive { get; set; }
        public List<ProductIngredient> Ingredients { get; set; } = new();
    }
}
