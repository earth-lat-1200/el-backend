using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthLat.Backend.Core.Dtos
{
    public class ChartDto
    {
        public List<DatasetDto> Datasets { get; set; }
        public string ChartType { get; set; }
        public string ChartTitle { get; set; }
        public string Description { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }
    }
}
