using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Collections.Generic;
using System.Globalization;
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

        public static int GetAdjustedSecondsSinceMidnight(this DateTime toAdjust, int timezoneOffset)
        {
            bool subtract = timezoneOffset < 0;
            return toAdjust.AddMinutes(timezoneOffset * (subtract ? -1 : 1)).GetSecondsSinceMidnight();
        }

        public static (string?,string?) GetHeaders(this HttpHeadersCollection headers)
        {
            var referenceDateTime = headers.FirstOrDefault(x => x.Key == "referencedatetime");
            var timezoneOffset = headers.FirstOrDefault(x => x.Key == "timezoneoffset");
            return (referenceDateTime.Value.FirstOrDefault(), timezoneOffset.Value.FirstOrDefault());
        }

        public static bool AreValidHeaders(this HttpHeadersCollection headers)
        {
            var (referenceDateTime, timezoneOffset) = headers.GetHeaders();
            if (referenceDateTime == null || timezoneOffset == null)
            {
                return false;
            }
            if (!DateTime.TryParseExact(referenceDateTime, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                       DateTimeStyles.None, out _) || !int.TryParse(timezoneOffset, out _))
            {
                return false;
            }
            return true;
        }
    }
}
