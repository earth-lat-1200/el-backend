using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthLat.Backend.Function
{
    public class Station
    {
        public string Id {  get; set; } = Guid.NewGuid().ToString("n");
    }
}
