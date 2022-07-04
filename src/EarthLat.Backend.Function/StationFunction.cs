using AutoMapper;
using EarthLat.Backend.Core.Exceptions;
using EarthLat.Backend.Core.Interfaces;
using EarthLat.Backend.Core.KeyManagement;
using EarthLat.Backend.Core.Models;
using EarthLat.Backend.Function.Dtos;
using EarthLat.Backend.Function.Extension;
using EarthLat.Backend.Function.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Configuration;
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
        private readonly ISundialLogic _sundialLogic;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly KeyManagementService _keyManagementService;
        private readonly IWebCamContentDtoValidator _webCamContentDtoValidator;

        public StationFunction(
            ISundialLogic sundialLogic,
            IMapper mapper,
            IConfiguration configuration,
            IWebCamContentDtoValidator webCamContentDtoValidator,
            KeyManagementService keyManagementService
            )
        {
            _sundialLogic = sundialLogic ?? throw new ArgumentNullException(nameof(sundialLogic));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _keyManagementService = keyManagementService ?? throw new ArgumentNullException(nameof(keyManagementService));
            this._webCamContentDtoValidator = webCamContentDtoValidator ?? throw new ArgumentNullException(nameof(webCamContentDtoValidator));
        }

        [Function(nameof(GetAllStations))]
        [OpenApiOperation(operationId: nameof(GetAllStations), tags: new[] { "Frontend API" }, Summary = "Gets all stations.", Description = "Get all station infos of the available stations.")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = Application.FunctionsKeyHeader, In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(IEnumerable<StationInfoDto>), Description = "All stations in the system.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Resource not found.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized access.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Conflict, Description = "Internal data layer conflict.")]
        public async Task<ActionResult<IEnumerable<StationInfoDto>>> GetAllStations(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData request)
        {
            List<Station> result = (await _sundialLogic.GetAllStationsAsync()).ToList();

            return result is null || result?.Count < 1
                ? new NotFoundResult()
                : new OkObjectResult(_mapper.Map<IEnumerable<StationInfoDto>>(result));
        }

        [Function(nameof(PushStationInfos))]
        [OpenApiOperation(operationId: nameof(PushStationInfos), tags: new[] { "Raspberry Pi API" },
            Summary = "Push station infos to the backend", Description = "Push the informations from the python client to the backend (stationInfo, imageTotal, imageDetail).")]
        [OpenApiParameter("stationId", In = ParameterLocation.Path)]
        [OpenApiRequestBody("applicaton/json", typeof(WebCamContentDto), Description = "The body consists of the stationInfo, the imageTotal and the imageDetail in a json format.")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = Application.FunctionsKeyHeader, In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(RemoteConfig),
            Summary = "The OK response", Description = "The OK response returns the remotConfig for the specific station.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Bad Request response.", Description = "Request could not be processed.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized access or permission for station denied.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Conflict, Description = "Internal data layer conflict.")]
        public async Task<IActionResult> PushStationInfos(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "{stationId}/Push")] HttpRequestData request, string stationId)
        {
            try
            {
                var header = request.GetHeaderKey();
                await _keyManagementService.CheckPermission(header, stationId);

                string requestBody = string.Empty;
                using (StreamReader streamReader = new(request.Body))
                    requestBody = await streamReader.ReadToEndAsync();

                var webCamContent = JsonConvert.DeserializeObject<WebCamContentDto>(requestBody);

                if (webCamContent.StationId.ToLower() != stationId.ToLower())
                {
                    throw new DataProcessException("Station id check failed.");
                }

                _webCamContentDtoValidator.IsValid(webCamContent);
                _webCamContentDtoValidator.IsValid(webCamContent.Status);

                var remoteConfig = await _sundialLogic.AddAsync(_mapper.Map<Station>(webCamContent), _mapper.Map<Images>(webCamContent));

                return new OkObjectResult(remoteConfig);
            }
            catch (ValidationException e) { return new BadRequestObjectResult(e.Message); }
            catch (UnauthorizedException) { return new UnauthorizedResult(); }
            catch (DataProcessException e) { return new BadRequestObjectResult(e.Message); }
            catch (Exception e) { return new ConflictObjectResult(e.Message); }
        }

        [Function(nameof(UpdateRemoteConfig))]
        [OpenApiOperation(operationId: nameof(UpdateRemoteConfig), tags: new[] { "Raspberry Pi API" }, Summary = "Update remote config.", Description = "Update the remote config of a station.")]
        [OpenApiParameter("stationId", In = ParameterLocation.Path)]
        [OpenApiRequestBody("applicaton/json", typeof(RemoteConfigDto), Description = "The body consists of the stationInfo, the imageTotal and the imageDetail in a json format.")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = Application.FunctionsKeyHeader, In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(RemoteConfigDto), Summary = "The OK response", Description = "The OK response returns the remotConfig for the specific station.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Bad Request response.", Description = "Request could not be processed.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized access or permission for station denied.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Conflict, Description = "Internal data layer conflict.")]
        public async Task<ActionResult<RemoteConfigDto>> UpdateRemoteConfig(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "{stationId}/Update")] HttpRequestData request, string stationId)
        {
            var station = await _sundialLogic.GetRemoteConfigById(stationId);

            if (station is null)
            {
                return new NotFoundResult();
            }

            string requestBody = string.Empty;
            using (StreamReader streamReader = new(request.Body))
            {
                requestBody = await streamReader.ReadToEndAsync();
            }

            var remoteConfigDto = JsonConvert.DeserializeObject<RemoteConfigDto>(requestBody);
            var remoteConfig = await _sundialLogic.AddOrUpdateRemoteConfigAsync(_mapper.Map<RemoteConfig>(remoteConfigDto), stationId);

            return new OkObjectResult(_mapper.Map<RemoteConfigDto>(remoteConfig));
        }

        [Function(nameof(GetLatestImageAsPictureById))]
        [OpenApiOperation(operationId: nameof(GetLatestImageAsPictureById), tags: new[] { "Frontend API" }, Summary = "Gets current image detail by stationId.", Description = "Get the latest created detail image of a station.")]
        [OpenApiParameter("imageType", In = ParameterLocation.Path)]
        [OpenApiParameter("stationId", In = ParameterLocation.Path)]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = Application.FunctionsKeyHeader, In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "image/jpeg", bodyType: typeof(byte[]), Description = "The latest detail image of a station as a picture.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Bad Request response.", Description = "Request could not be processed.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Resource not found.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized access.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Conflict, Description = "Internal data layer conflict.")]
        public async Task<byte[]> GetLatestImageAsPictureById(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "GetLatestImageAsPictureById/{imageType}/{stationId}")] HttpRequestData request, string imageType)
        {
            string id = request.FunctionContext
                       .BindingContext
                       .BindingData["stationId"]
                       .ToString();

            var images = await _sundialLogic.GetLatestImagesByIdAsync(id);
            byte[] image = imageType == "detail" ? images?.ImgDetail : images?.ImgTotal;

            return image ?? Array.Empty<byte>();
        }

        [Function(nameof(GetLatestDetailImageById))]
        [OpenApiOperation(operationId: nameof(GetLatestDetailImageById), tags: new[] { "Frontend API" }, Summary = "Gets current image detail by stationId.", Description = "Get the latest created detail image of a station.")]
        [OpenApiParameter("id", In = ParameterLocation.Query, Description = "The station identifier.")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = Application.FunctionsKeyHeader, In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ImgDto), Description = "The latest detail image of a station.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Bad Request response.", Description = "Request could not be processed.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Resource not found.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized access.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Conflict, Description = "Internal data layer conflict.")]
        public async Task<IActionResult> GetLatestDetailImageById(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData request)
        {
            string id = request.FunctionContext
                                  .BindingContext
                                  .BindingData["id"]
                                  .ToString();

            var images = await _sundialLogic.GetLatestImagesByIdAsync(id);

            return images is null ? new NotFoundResult() : new OkObjectResult(new ImgDto() { Img = images.ImgDetail });
        }

        [Function(nameof(GetLatestTotalImageById))]
        [OpenApiOperation(operationId: nameof(GetLatestTotalImageById), tags: new[] { "Frontend API" }, Summary = "Gets current total image by stationId.", Description = "Get the latest created total image of a station.")]
        [OpenApiParameter("id", In = ParameterLocation.Query, Description = "The station identifier.")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = Application.FunctionsKeyHeader, In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ImgDto), Description = "The latest total image of a station.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Bad Request response.", Description = "Request could not be processed.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Resource not found.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized access.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Conflict, Description = "Internal data layer conflict.")]
        public async Task<byte[]> GetLatestTotalImageById(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData request)
        {
            string id = request.FunctionContext
                                   .BindingContext
                                   .BindingData["id"]
                                   .ToString();
            var images = await _sundialLogic.GetLatestImagesByIdAsync(id);

            return images.ImgTotal;
        }

        [Function(nameof(CleanUp))]
        [OpenApiRequestBody("applicaton/json", typeof(CleanUpDto), Description = "Contains the timestamp for the clean up process and the optional station id.")]
        [OpenApiOperation(operationId: nameof(CleanUp), tags: new[] { "Frontend API" }, Summary = "Clean up images store.", Description = "Clean up.")]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = Application.FunctionsKeyHeader, In = OpenApiSecurityLocationType.Header)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "Deletion count.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Bad Request response.", Description = "Request could not be processed.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Resource not found.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized access.")]
        public async Task<IActionResult> CleanUp(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "CleanUp")] HttpRequestData request)
        {
            try
            {
                string requestBody = await request.GetRequestBody();
                var cleanUp = JsonConvert.DeserializeObject<CleanUpDto>(requestBody);
                var count = await _sundialLogic.CleanUp(cleanUp.DeleteAllBeforeTimestamp, cleanUp.StationId);

                return new OkObjectResult($"{count} elements deleted.");
            }
            catch (Exception e) { return new ConflictObjectResult(e.Message); }
        }
    }
}

