using AutoMapper;
using EarthLat.Backend.Core.Dtos;
using EarthLat.Backend.Core.Interfaces;
using EarthLat.Backend.Core.Models;
using EarthLat.Backend.Function.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace EarthLat.Backend.Function
{
    public class StationFunction
    {
        private readonly ISundialLogic _stationLogic;
        private readonly IMapper _mapper;

        private static readonly RemoteConfig exampleConfig = new RemoteConfig
        {
            IsCamOffline = false,
            Period = TimeSpan.FromSeconds(2),
            IsSeries = false,
            IsZoomMove = false,
            IsZoomDrawRect = false,
            ZoomCenterPerCX = 0,
            ZoomCenterPerCy = 0
        };

        public StationFunction(ISundialLogic stationLogic, IMapper mapper)
        {
            _stationLogic = stationLogic ?? throw new ArgumentNullException(nameof(stationLogic));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [FunctionName(nameof(GetAllStations))]
        [OpenApiOperation(operationId: nameof(GetAllStations), tags: new[] { "Frontend API" }, Summary = "Gets all stations.", Description = "Get all station infos of the available stations.")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<StationInfoDto>), Description = "All stations in the system.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Resource not found.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized access.")]
        public async Task<ActionResult<IEnumerable<StationInfoDto>>> GetAllStations(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "stations")] HttpRequest request)
        {
            List<Station> result = (await _stationLogic.GetAllStationsAsync()).ToList();

            return result is null || result?.Count < 1 ? new NotFoundResult() : new OkObjectResult(_mapper.Map<IEnumerable<StationInfoDto>>(result));
        }

        [FunctionName(nameof(GetLatestDetailImageById))]
        [OpenApiOperation(operationId: nameof(GetLatestDetailImageById), tags: new[] { "Frontend API" }, Summary = "Gets current image detail by stationId.", Description = "Get the latest created detail image of a station.")]
        [OpenApiParameter("id", In = ParameterLocation.Query, Description = "The station identifier.")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ImgDto), Description = "The latest detail image of a station.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Bad Request response.", Description = "Request could not be processed.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Resource not found.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized access.")]
        public async Task<IActionResult> GetLatestDetailImageById(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "stations/images/detail/{id}")] HttpRequest request)
        {
            string id = request.Query["id"];
            var images = await _stationLogic.GetLatestImagesByIdAsync(id);

            return images is null ? new NotFoundResult() : new OkObjectResult(new ImgDto() { Img = images.ImgDetail });
        }

        [FunctionName(nameof(GetLatestTotalImageById))]
        [OpenApiOperation(operationId: nameof(GetLatestTotalImageById), tags: new[] { "Frontend API" }, Summary = "Gets current total image by stationId.", Description = "Get the latest created total image of a station.")]
        [OpenApiParameter("id", In = ParameterLocation.Query, Description = "The station identifier.")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ImgDto), Description = "The latest total image of a station.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Bad Request response.", Description = "Request could not be processed.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Resource not found.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized access.")]
        public async Task<ActionResult<ImgDto>> GetLatestTotalImageById(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "stations/images/total/{id}")] HttpRequest request)
        {
            string id = request.Query["id"];
            var images = await _stationLogic.GetLatestImagesByIdAsync(id);

            return images is null ? new NotFoundResult() : new OkObjectResult(new ImgDto() { Img = images.ImgTotal });
        }

        [FunctionName(nameof(PushStationInfos))]
        [OpenApiOperation(operationId: nameof(PushStationInfos), tags: new[] { "Raspberry Pi API" }, 
            Summary = "Push station infos to the backend", Description = "Push the informations from the python client to the backend (stationInfo, imageTotal, imageDetail).")]
        [OpenApiRequestBody("applicaton/json", typeof(WebCamContentDto), Description = "The body consists of the stationInfo, the imageTotal and the imageDetail in a json format.")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(RemoteConfig), 
            Summary = "The OK response" , Description = "The OK response returns the remotConfig for the specific station.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Bad Request response." , Description = "Request could not be processed.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized access.")]
        public async Task<IActionResult> PushStationInfos(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "stations/PushStationInfos")] HttpRequest request)
        {
            string requestBody = String.Empty;
            using (StreamReader streamReader = new(request.Body))
            {
                requestBody = await streamReader.ReadToEndAsync();
            }

            var webCamContent = JsonConvert.DeserializeObject<WebCamContentDto>(requestBody);
            var remoteConfig = await _stationLogic.AddAsync(_mapper.Map<Station>(webCamContent), _mapper.Map<Images>(webCamContent));

            return new OkObjectResult(remoteConfig);
        }
    }
}

