﻿using EarthLat.Backend.Core.Models;

namespace EarthLat.Backend.Core.Extensions
{
    internal static class  ModelsExtensions
    {
        public static string RemoteConfigRowKeyPostfix = "_config";

        internal static void SetImagesRowKey(this Images images)
        {
            images.RowKey = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
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

        internal static float ParseToFloat(this string numberString)
        {
            try
            {
                return float.Parse(numberString.Replace(".", ","));
            }
            catch(Exception)
            {
                return 99.9f;
            }
        }
    }
}
