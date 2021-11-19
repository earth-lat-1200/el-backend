using EarthLat.Backend.Core.BusinessLogic;
using EarthLat.Backend.Core.Dtos;
using EarthLat.Backend.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace EarthLat.Backend.Function
{
    public class StationFunction
    {
        private readonly IStationLogic stationLogic;
        private static readonly Station exampleStation = new Station
        {
            ImgTotal = null,
            ImgDetail = null,
            StationId = "AT101",
            StationName = "SDC_Developer",
            SundialName = "Developer Sundial",
            NearbyPublicInstitute = "Great Public Building",
            WebsiteUrl = "http://example.com",
            SundialInfo = "A short description of the sundial, the project, interesting detail, ...",
            TransferType = "RaspberryPi",
            TeamName = "team name, members, ...",
            WebcamType = "RaspberryPi + Cam",
            Location = "City, Country",
            OrganizationalForm = "The organization of the team",
            Latitude = 48.30,
            Longitude = 10.00
        };
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

        public StationFunction(IStationLogic stationLogic)
        {
            this.stationLogic = stationLogic ?? throw new ArgumentNullException(nameof(stationLogic));
        }

        [FunctionName(nameof(GetAllStationInfos))]
        [OpenApiOperation(operationId: nameof(GetAllStationInfos), tags: new[] { "Frontend" }, Summary = "Gets all station infos", Description = "Get all station infos of the available stations")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> GetAllStationInfos(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "stations")] IEnumerable<StationInfoDto> allStationInfos,
            ILogger log)
        {
            log.LogInformation("Getting all stationInfos");

            var stationInfos = (await stationLogic.GetAllStationInfos()).Select(s => s.ToStationInfoDto());
            if (stationInfos == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(stationInfos);
        }

        [FunctionName(nameof(GetImgTotalById))]
        [OpenApiOperation(operationId: nameof(GetImgTotalById), tags: new[] { "Frontend" }, Summary = "Get current image total by stationId", Description = "Get the current total image of a station")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> GetImgTotalById(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "stations/images/total/{id}")] StationImgTotalDto stationImgTotal,
            ILogger log, string id)
        {
            log.LogInformation("Getting imgTotal from station by id");

            var imgTotal = (await stationLogic.GetImgTotalById(id)).ToStationImgTotalDto();
            if (imgTotal == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(imgTotal);
        }

        [FunctionName(nameof(GetImgDetailById))]
        [OpenApiOperation(operationId: nameof(GetImgDetailById), tags: new[] { "Frontend" }, Summary = "Get current image detail by stationId", Description = "Get the current detail image of a station")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> GetImgDetailById(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "stations/images/detail/{id}")] StationImgDetailDto stationImgDetail,
            ILogger log, string id)
        {
            log.LogInformation("Getting imgTotal from station by id");

            var imgDetail = (await stationLogic.GetImgDetailById(id)).ToStationImgDetailDto();
            if (imgDetail == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(imgDetail);
        }

        [FunctionName(nameof(GetAllImgTotalById))]
        [OpenApiOperation(operationId: nameof(GetAllImgTotalById), tags: new[] { "Frontend" }, Summary = "Get all total images by stationId", Description = "Get all total images of a station")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> GetAllImgTotalById(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "stations/images/totals/{id}")] IEnumerable<StationImgTotalDto> stationImgTotals,
            ILogger log, string id)
        {
            log.LogInformation("Getting all imgTotals from station by id");

            var imgTotals = (await stationLogic.GetAllImgTotalByStationId(id)).Select(s => s.ToStationImgTotalDto());
            if (imgTotals == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(imgTotals);
        }

        [FunctionName(nameof(GetAllImgDetailById))]
        [OpenApiOperation(operationId: nameof(GetAllImgDetailById), tags: new[] { "Frontend" }, Summary = "Get all detail images by stationId", Description = "Get all detail images of a station")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> GetAllImgDetailById(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "stations/images/details/{id}")] IEnumerable<StationImgDetailDto> stationImgDetails,
            ILogger log, string id)
        {
            log.LogInformation("Getting all imgTotals from station by id");

            var imgDetails = (await stationLogic.GetAllImgDetailByStationId(id)).Select(s => s.ToStationImgTotalDto());
            if (imgDetails == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(imgDetails);
        }

        [FunctionName(nameof(PushStationInfos))]
        [OpenApiOperation(operationId: nameof(PushStationInfos), tags: new[] { "PythonClient" }, Summary = "Push station infos to the backend", Description = "Push the informations from the python client to the backend (stationInfo, imageTotal, imageDetail)")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(string), Description = "The body consists of the stationInfo, the imageTotal and the imageDetail in a json format")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(RemoteConfig), Summary = "The OK response" , Description = "The OK response returns the remotConfig for the specific station")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Bad Request response" , Description = "Inquiry could not be processed")]
        public async Task<IActionResult> PushStationInfos(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "stations")] RemoteConfig rConfig,
            ILogger log, string id)
        {
            log.LogInformation("Getting all imgTotals from station by id");

            var remoteConfig = (await stationLogic.PushStationInfos());
            if (remoteConfig == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(remoteConfig);
        }

        /*
        [FunctionName(nameof(GetStationById))]
        [OpenApiOperation(operationId: "GetStationById", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public IActionResult GetStationById(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "station/{id}")] HttpRequest req,
            ILogger log, string id)
        {
            log.LogInformation("Getting Station by id");

            var station = new Object(); // Stations.FirstOrDefault(t => t.Id == id);
            if (station == null)
            {
                return new NotFoundResult();
            }
            return new OkObjectResult(station);
        }

        [FunctionName(nameof(GetByLocation))]
        [OpenApiOperation(operationId: "GetByLocation", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "longitude", In = ParameterLocation.Query, Required = true, Type = typeof(double), Description = "Longitude")]
        [OpenApiParameter(name: "latitude", In = ParameterLocation.Query, Required = true, Type = typeof(double), Description = "Latitude")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> GetByLocation(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = nameof(GetByLocation))] HttpRequest req, ILogger log)
        {
            await Task.CompletedTask;
            if (!double.TryParse(req.Query["longitude"], out var longitude) && !double.TryParse(req.Query["latitude"], out var latitude))
                {
                throw new ArgumentException($"{nameof(longitude)} or {nameof(latitude)} are invalid.");
            }

            return new OkResult();
        }*/
    }
}

