using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using EarthLat.Backend.Core.BusinessLogic;
using EarthLat.Backend.Core.Models;
using EarthLat.Backend.Core.Extensions;
using EarthLat.Backend.Core.Dtos;
using EarthLat.Backend.Function.Extension;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Newtonsoft.Json;
using EarthLat.Backend.Core.JWT;

namespace EarthLat.Backend.Function
{
    public class StatisticFunctions
    {
        private readonly StatisticService statisticService;
        private readonly JwtValidator validator;
        private readonly string INVALID_HEADER_MESSAGE = "invalid Headers";
        private readonly string NO_DATA_FOUND_MESSAGE = "no data found";
        public StatisticFunctions(StatisticService statisticService, JwtValidator validator)
        {
            this.statisticService = statisticService;
            this.validator = validator;
        }


        [Function(nameof(Authenticate))]
        [OpenApiRequestBody("applicaton/json", typeof(UserCredentials), Description = "Contains username and password of the user who attempts to login")]
        [OpenApiOperation(operationId: nameof(Authenticate), tags: new[] { "Frontend API" }, Summary = "Authenticate the user")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(string), Description = "The JWT of the user matching the credentials")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Bad Request response.", Description = "Request could not be processed.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Resource not found.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized access.")]
        public async Task<ActionResult<string>> Authenticate(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "Authenticate")] HttpRequestData request)
        {
            try
            {
                string requestBody = await request.GetRequestBody();
                var credentials = JsonConvert.DeserializeObject<UserCredentials>(requestBody);
                var jwt = await statisticService.AuthenticateAsync(credentials);
                return (jwt == null)
                    ? new UnauthorizedObjectResult("Username or Password not found")
                    : new OkObjectResult(jwt);
            }
            catch (Exception)
            {
                return new NotFoundResult();
            }
        }

        [Function(nameof(GetBroadcastTimesAsync))]
        [OpenApiOperation(operationId: nameof(GetBroadcastTimesAsync), tags: new[] { "Frontend API" }, Summary = "Get the sending times of a station")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ChartDto), Description = "The start- and endtime(s) of a/all stations sending activity on a certain day")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Bad Request response.", Description = "Request could not be processed.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Resource not found.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized access.")]
        public async Task<ActionResult<ChartDto>> GetBroadcastTimesAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "BroadcastTimes")] HttpRequestData request)
        {
            try
            {
                validator.Validate(request);
                if (!validator.IsValid)
                {
                    return new UnauthorizedResult();
                }
                if (!request.Headers.AreValidHeaders())
                {
                    return new NotFoundObjectResult(INVALID_HEADER_MESSAGE);
                }
                var (startReferenceDate, endReferenceDate) = request.Headers.GetHeaders();
                var sendTimes = await statisticService.GetBroadcastTimesAsync(validator, startReferenceDate, endReferenceDate);
                return (sendTimes == null)
                    ? new NotFoundObjectResult(NO_DATA_FOUND_MESSAGE)
                    : new OkObjectResult(sendTimes);
            }
            catch (Exception e)
            {
                return new ConflictObjectResult(e.Message);
            }
        }

        [Function(nameof(GetAverageTemperaturePerHourAsync))]
        [OpenApiOperation(operationId: nameof(GetAverageTemperaturePerHourAsync), tags: new[] { "Frontend API" }, Summary = "Get the sending times of a station")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ChartDto), Description = "The average temperature of the uploaded images of a/all station(s) per hour")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Bad Request response.", Description = "Request could not be processed.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Resource not found.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized access.")]
        public async Task<ActionResult<ChartDto>> GetAverageTemperaturePerHourAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "TemperatureValues")] HttpRequestData request)
        {
            try
            {
                validator.Validate(request);
                if (!validator.IsValid)
                {
                    return new UnauthorizedResult();
                }
                if (!request.Headers.AreValidHeaders())
                {
                    return new NotFoundObjectResult(INVALID_HEADER_MESSAGE);
                }
                var (startReferenceDate, endReferenceDate) = request.Headers.GetHeaders();
                var brightnessValues = await statisticService.GetTemperatrueValuesPerHourAsync(validator, startReferenceDate, endReferenceDate);
                return (brightnessValues == null)
                    ? new NotFoundObjectResult(NO_DATA_FOUND_MESSAGE)
                    : new OkObjectResult(brightnessValues);
            }
            catch (Exception e)
            {
                return new ConflictObjectResult(e.Message);
            }
        }

        [Function(nameof(GetImagesPerHourAsync))]
        [OpenApiOperation(operationId: nameof(GetImagesPerHourAsync), tags: new[] { "Frontend API" }, Summary = "Get the sending times of a station")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ChartDto), Description = "The uploaded images per hour of a/all station(s) on a certain day")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Bad Request response.", Description = "Request could not be processed.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Resource not found.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized access.")]
        public async Task<ActionResult<ChartDto>> GetImagesPerHourAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "ImagesPerHour")] HttpRequestData request)
        {
            try
            {
                validator.Validate(request);
                if (!validator.IsValid)
                {
                    return new UnauthorizedResult();
                }
                if (!request.Headers.AreValidHeaders())
                {
                    return new NotFoundObjectResult(INVALID_HEADER_MESSAGE);
                }
                var (startReferenceDate, endReferenceDate) = request.Headers.GetHeaders();
                var sendTimes = await statisticService.GetImagesPerHourAsync(validator, startReferenceDate, endReferenceDate);
                return (sendTimes == null)
                    ? new NotFoundObjectResult(NO_DATA_FOUND_MESSAGE)
                    : new OkObjectResult(sendTimes);
            }
            catch (Exception e)
            {
                return new ConflictObjectResult(e.Message);
            }
        }

        [Function(nameof(GetAverageBrightnessPerHourAsync))]
        [OpenApiOperation(operationId: nameof(GetAverageBrightnessPerHourAsync), tags: new[] { "Frontend API" }, Summary = "Get the sending times of a station")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ChartDto), Description = "The average brightness of the uploaded images of a/all station(s) per hour")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Bad Request response.", Description = "Request could not be processed.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Resource not found.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized access.")]
        public async Task<ActionResult<ChartDto>> GetAverageBrightnessPerHourAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "BrightnessValues")] HttpRequestData request)
        {
            try
            {
                validator.Validate(request);
                if (!validator.IsValid)
                {
                    return new UnauthorizedResult();
                }
                if (!request.Headers.AreValidHeaders())
                {
                    return new NotFoundObjectResult(INVALID_HEADER_MESSAGE);
                }
                var (startReferenceDate, endReferenceDate) = request.Headers.GetHeaders();
                var brightnessValues = await statisticService.GetBrightnessValuesPerHourAsync(validator, startReferenceDate, endReferenceDate);
                return (brightnessValues == null)
                    ? new NotFoundObjectResult(NO_DATA_FOUND_MESSAGE)
                    : new OkObjectResult(brightnessValues);
            }
            catch (Exception e)
            {
                return new ConflictObjectResult(e.Message);
            }
        }
    }
}