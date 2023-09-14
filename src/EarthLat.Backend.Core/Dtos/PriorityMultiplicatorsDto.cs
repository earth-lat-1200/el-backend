using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthLat.Backend.Core.Dtos
{
    public class PriorityMultiplicatorsDto
    {
        public PriorityMultiplicatorsDto() { }
        public PriorityMultiplicatorsDto(double priorityMultiplicator, double distanzeMultiplicator, double randomMultiplicator)
        {
            this.priorityMultiplicator = priorityMultiplicator;
            this.distanceMultiplicator = distanzeMultiplicator;
            this.randomMultiplicator = randomMultiplicator;
        }
        public double priorityMultiplicator { get; set; }
        public double distanceMultiplicator { get; set; }
        public double randomMultiplicator { get; set; }
    }
}
