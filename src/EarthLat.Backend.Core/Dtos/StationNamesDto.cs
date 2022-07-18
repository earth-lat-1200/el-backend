using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthLat.Backend.Core.Dtos
{
    public class StationNamesDto
    {
        public List<string> StationNames { get; set; }
        public string UserStationName { get; set; }
    }
}
