using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthLat.Backend.Core.Dtos
{
    public class LineChartDto
    {
        public string Name { get; set; }
        public Coordinate[] Values { get; set; }
    }
}
