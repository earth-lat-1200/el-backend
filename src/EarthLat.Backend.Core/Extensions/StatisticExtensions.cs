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

        public static bool AreValidHeaders(this HttpHeadersCollection headers)
        {
            var (startReferenceDate, endReferenceDate) = headers.GetHeaders();
            if (startReferenceDate == null || endReferenceDate == null)
            {
                return false;
            }
            if (!DateTime.TryParseExact(startReferenceDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                       DateTimeStyles.None, out _)
               || !DateTime.TryParseExact(endReferenceDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                       DateTimeStyles.None, out _))
            {
                return false;
            }
            var parsedStartReferenceDate = startReferenceDate.ParseToDate();
            var parsedEndReferenceDate = endReferenceDate.ParseToDate();
            if (parsedStartReferenceDate > parsedEndReferenceDate)
            {
                return false;
            }
            return true;
        }
        public static (string?, string?) GetHeaders(this HttpHeadersCollection headers)
        {
            var startDate = headers.FirstOrDefault(x => x.Key == "startdate");
            var endDate = headers.FirstOrDefault(x => x.Key == "enddate");
            return (startDate.Value.FirstOrDefault(), endDate.Value.FirstOrDefault());
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
