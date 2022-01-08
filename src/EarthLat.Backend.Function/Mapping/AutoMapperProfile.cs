using AutoMapper;
using EarthLat.Backend.Core.Models;
using EarthLat.Backend.Function.Dtos;
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
                .ForMember(dest => dest.StationId, opt => opt.MapFrom(src => src.PartitionKey));

            CreateMap<RemoteConfig, RemoteConfigDto>(MemberList.None)
                .ReverseMap();
        }
    }
}