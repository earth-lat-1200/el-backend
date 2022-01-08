using EarthLat.Backend.Core.Models;

namespace EarthLat.Backend.Core.Extensions
{
    internal static class  ModelsExtensions
    {
        public static string RemoteConfigRowKeyPostfix = "_config";

        internal static void SetStationRowKey(this Station station)
        {
            station.RowKey = $"{station.Longitude}_{station.Latitude}";
        }

        internal static void SetImagesRowKey(this Images images)
        {
            images.RowKey = Guid.NewGuid().ToString("N");
        }

        internal static void SetRemoteConfigKeys(this RemoteConfig remoteConfig, string stationId)
        {
            if (string.IsNullOrWhiteSpace(stationId))
            {
                throw new ArgumentException($"'{nameof(stationId)}' cannot be null or whitespace.", nameof(stationId));
            }

            remoteConfig.PartitionKey = stationId;
            remoteConfig.RowKey = $"{remoteConfig.PartitionKey}{RemoteConfigRowKeyPostfix}";
        }
    }
}
