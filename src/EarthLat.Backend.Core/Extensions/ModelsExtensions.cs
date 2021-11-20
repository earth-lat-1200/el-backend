using EarthLat.Backend.Core.Models;

namespace EarthLat.Backend.Core.Extensions
{
    internal static class  ModelsExtensions
    {
        internal static void SetStationRowKey(this Station station)
        {
            station.RowKey = $"{station.Longitude}_{station.Latitude}";
        }

        internal static void SetImagesRowKey(this Images images)
        {
            images.RowKey = Guid.NewGuid().ToString("N");
        }
    }
}
