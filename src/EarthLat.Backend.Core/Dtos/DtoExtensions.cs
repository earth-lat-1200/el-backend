using EarthLat.Backend.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthLat.Backend.Core.Dtos
{
    public static class DtoExtensions
    {
        public static StationInfoDto? ToStationInfoDto(this Station station)
        {
            if(station == null)
            {
                return null;
            }

            return new StationInfoDto()
            {
                StationName = station.StationName,
                StationId = station.StationId,
                SundialName = station.SundialName,
                Location = station.Location,
                Latitude = station.Latitude,
                Longitude = station.Longitude,
                WebcamType = station.WebcamType,
                TransferType = station.TransferType,
                SundialInfo = station.SundialInfo,
                WebsiteUrl = station.WebsiteUrl,
                TeamName = station.TeamName,
                NearbyPublicInstitute = station.NearbyPublicInstitute,
                OrganizationalForm = station.OrganizationalForm
            };
        }

        public static StationImgTotalDto? ToStationImgTotalDto(this Station station)
        {
            if (station == null)
            {
                return null;
            }

            return new StationImgTotalDto()
            {
                ImgTotal = station.ImgTotal
            };
        }

        public static StationImgDetailDto? ToStationImgDetailDto(this Station station)
        {
            if (station == null)
            {
                return null;
            }

            return new StationImgDetailDto()
            {
                ImgDetail = station.ImgDetail
            };
        }
    }
}
