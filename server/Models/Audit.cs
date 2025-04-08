using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.Models
{
    public class Audit
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string? Name { get; set; }
        public string? Action { get; set; }
        public string? Description { get; set; }
        public string? PrevValue { get; set; }
        public string? NewValue { get; set; }
        public string? IpAddress { get; set; }
        public string? Entity { get; set; }
        public int EntityId { get; set; }
    }
}
