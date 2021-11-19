using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthLat.Backend.Core.BusinessLogic
{
    public interface IStationLogic
    {
        Task<IEnumerable<Station>> GetAllStationInfos();
        Task<Station> GetImgTotalById(string stationId);
        Task<Station> GetImgDetailById(string stationId);
        Task<IEnumerable<Station>> GetAllImgTotalByStationId(string stationId);
        Task<IEnumerable<Station>> GetAllImgDetailByStationId(string stationId);
        Task<RemoteConfig> PushStationInfos();
        /*Task<IEnumerable<Station>> GetStations();
        Task<Station> GetStationById(string stationId);
        Task<Station> GetStationByLocation(double longitude, double latitude);*/
    }
}
