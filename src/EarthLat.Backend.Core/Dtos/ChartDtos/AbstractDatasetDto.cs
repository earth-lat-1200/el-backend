using EarthLat.Backend.Core.Dtos.ChartDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthLat.Backend.Core.Dtos
{
    public class DatasetDto
    {
        public string StationName { get; set; }
        public List<AbstractValuesDto> Values { get; set; }
    }
}
