using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }
        public int TransId { get; set; }
        public decimal AmountPaid { get; set; }
        public string? PaymentMethod { get; set; }
        public string? ReferenceNumber { get; set; }
        public DateTime PaymentTime { get; set; }
        public decimal ChangeAmount { get; set; }
        public string? Notes { get; set; }
    }
}
