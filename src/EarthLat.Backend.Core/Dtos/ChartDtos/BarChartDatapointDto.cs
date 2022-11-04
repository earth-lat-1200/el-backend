using EarthLat.Backend.Core.Dtos.ChartDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthLat.Backend.Core.Dtos
{
    public class BarChartDatapointDto : AbstractValuesDto
    {
        public string Start { get; set; }
        public string End { get; set; }
    }
}
