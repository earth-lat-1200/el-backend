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

        public static DateTime ParseToDate(this string date)
        {
            return DateTime.ParseExact(
                date,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None).Date;
        }
        public static DateTime ParseToDateTime(this string date)
        {
            return DateTime.ParseExact(
                date,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None);
        }
        public static DateTime GetDateTime(this long timestamp)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime;
        }

        public static bool AreValidHeaders(this HttpHeadersCollection headers)
        {
            var referenceDate = headers.GetHeader();
            if (referenceDate == null || !DateTime.TryParseExact(referenceDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                       DateTimeStyles.None, out _))
            {
                return false;
            }
            return true;
        }
        public static string? GetHeader(this HttpHeadersCollection headers)
        {
            var referenceDate = headers.FirstOrDefault(x => x.Key == "referencedate");
            return (referenceDate.Value.FirstOrDefault());
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
