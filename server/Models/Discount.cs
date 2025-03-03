using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.Models
{
    public class Discount
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public int VatExempt { get; set; }
        public string ApplicableTo { get; set; } = string.Empty;
        public int Status { get; set; }
        public List<int>? CategoryIds { get; set; }
    }
}
