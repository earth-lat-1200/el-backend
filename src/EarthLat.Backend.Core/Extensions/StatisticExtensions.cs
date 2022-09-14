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
        public static int GetSecondsSinceStartTime(this DateTime date, int timezoneOffset, string referenceDate)
        {
            var startDate = referenceDate.GetStartDate();
            var totalSeconds = date
                .AddMinutes(timezoneOffset)
                .Subtract(startDate);
            return (int)totalSeconds.TotalSeconds;
        }

        public static DateTime GetStartDate(this string referenceDate)
        {
            return DateTime.ParseExact(referenceDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None)
                .Date.AddDays(-1);
        }
        public static DateTime GetDateTime(this long timestamp)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
        }

        public static bool AreValidHeaders(this HttpHeadersCollection headers)
        {
            var (referenceDate, timezoneOffset) = headers.GetHeaders();
            if (referenceDate == null || timezoneOffset == null)
            {
                return false;
            }
            if (!DateTime.TryParseExact(referenceDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                       DateTimeStyles.None, out _) || !int.TryParse(timezoneOffset, out _))
            {
                return false;
            }
            return true;
        }
        public static (string?, string?) GetHeaders(this HttpHeadersCollection headers)
        {
            var referenceDate = headers.FirstOrDefault(x => x.Key == "referencedate");
            var timezoneOffset = headers.FirstOrDefault(x => x.Key == "timezoneoffset");
            return (referenceDate.Value.FirstOrDefault(), timezoneOffset.Value.FirstOrDefault());
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
