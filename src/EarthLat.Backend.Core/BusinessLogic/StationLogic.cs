using EarthLat.Backend.Core.Abstraction;
using EarthLat.Backend.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace EarthLat.Backend.Core.BusinessLogic
{
    public class StationLogic : IStationLogic
    {
        private readonly ILogger<IStationLogic> logger;
        private readonly IFileStorage fileStorage;

        public StationLogic(ILogger<IStationLogic> logger, IFileStorage fileStorage)
        {
            this.logger = logger;
            this.fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        }

        public Task<IEnumerable<Station>> GetAllImgDetailByStationId(string stationId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Station>> GetAllImgTotalByStationId(string stationId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Station>> GetAllStationInfos()
        {
            throw new NotImplementedException();
        }

        public Task<Station> GetImgDetailById(string stationId)
        {
            throw new NotImplementedException();
        }

        public Task<Station> GetImgTotalById(string stationId)
        {
            throw new NotImplementedException();
        }

        public Task<RemoteConfig> PushStationInfos()
        {
            throw new NotImplementedException();
        }
        /*
public async Station GetStationById(string stationId)
{
   Station? station = null;
   var stationInfo = fileStorage.Download(stationId, "stationInfo.txt");
   var imageTotal = fileStorage.Download(stationId, "imageTotal.txt");

   var stationInfoJson = Encoding.UTF8.GetString(stationInfo);
   if (stationInfoJson is not null)
   {
       station = Newtonsoft.Json.JsonConvert.DeserializeObject<Station>(stationInfoJson);
   }
   if(station is not null)
   {
       station.ImgTotal = imageTotal;
   } else
   {
       station = new Station { ImgTotal = imageTotal };
   }

   return station;

}

public Task<Station> GetStationByLocation(double longitude, double latitude)
{
   throw new NotImplementedException();
}

public Task<IEnumerable<Station>> GetStations()
{
   throw new NotImplementedException();
}
*/
    }
}
