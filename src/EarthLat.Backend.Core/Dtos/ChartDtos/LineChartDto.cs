﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthLat.Backend.Core.Dtos
{
    public class LineChartDto : AbstractChartDto
    {
        public string Name { get; set; }
        public double[] Values { get; set; }
    }
}