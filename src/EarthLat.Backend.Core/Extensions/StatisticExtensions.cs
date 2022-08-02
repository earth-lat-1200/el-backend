using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthLat.Backend.Core.Extensions
{
    public static class StatisticExtensions
    {
        public static DateTime EndOfDay(this DateTime date)
        {
            return date.Date.AddDays(1).AddTicks(-1);
        }

        public static int GetSecondsSinceMidnight(this DateTime date)
        {
            return (int) date.Subtract(date.Date).TotalSeconds;
        }
    }
}
