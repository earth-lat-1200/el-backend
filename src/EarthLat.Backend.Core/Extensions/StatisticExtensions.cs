using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace EarthLat.Backend.Core.Extensions
{
    public static class StatisticExtensions
    {
        public static int GetSecondsStartTime(this DateTime date, int timezoneOffset)
        {
            var cleanedStartDate = date.Date.AddDays(-1);
            var totalSeconds = date.AddMinutes(timezoneOffset).Subtract(cleanedStartDate);
            return (int) totalSeconds.TotalSeconds;
        }
        public static DateTime GetDateTimeFromTimestamp(this long timestamp)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
        }

        public static (string?, string?) GetHeaders(this HttpHeadersCollection headers)
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

        public static string ToBase64(this object toConvert)
        {
            using MemoryStream ms = new();
            new BinaryFormatter().Serialize(ms, toConvert);
            return Convert.ToBase64String(ms.ToArray());
        }

        public static T FromBase64<T>(this string toConvert)
        {
            byte[] bytes = Convert.FromBase64String(toConvert);
            using MemoryStream ms = new MemoryStream(bytes, 0, bytes.Length);
            ms.Write(bytes, 0, bytes.Length);
            ms.Position = 0;
            return (T)new BinaryFormatter().Deserialize(ms);
        }
    }
}
