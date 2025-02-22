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

        public string? productName { get; set; }

        public string? productDesc { get; set; }

        public decimal? productPrice { get; set; }

        public string? productImage { get; set; }
    }
}
