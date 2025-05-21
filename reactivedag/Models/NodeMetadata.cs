using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveDAG.Core.Models
{
    public class NodeMetadata
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime? LastUpdated { get; set; }
    }
}
