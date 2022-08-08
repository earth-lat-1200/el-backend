using AutoMapper;
using EarthLat.Backend.Core.Dtos;
using EarthLat.Backend.Core.Models;
using System;

namespace EarthLat.Backend.Function.Mapping
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<WebCamContentDto, Station>()
                .ForMember(dest => dest.RowKey, opt => opt.MapFrom(src => src.StationId));

            CreateMap<WebCamContentDto, Images>()
                .ForMember(dest => dest.PartitionKey, opt => opt.MapFrom(src => src.StationId));

            CreateMap<Station, StationInfoDto>()
                .ForMember(dest => dest.StationId, opt => opt.MapFrom(src => src.RowKey));

            CreateMap<RemoteConfig, RemoteConfigDto>(MemberList.None)
                .ReverseMap();
        }
    }
}