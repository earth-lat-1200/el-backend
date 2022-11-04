using EarthLat.Backend.Core.Dtos.ChartDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthLat.Backend.Core.Dtos
{
    public class LineChartDatapointDto : AbstractValuesDto
    {
        public string Timestamp { get; set; }
        public double Value { get; set; }
    }
}
