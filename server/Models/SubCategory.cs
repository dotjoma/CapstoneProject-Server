using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.Models
{
    public class SubCategory
    {
        public int scId { get; set; }

        public int catId { get; set; }

        public string scName { get; set; } = string.Empty;
    }
}
