using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using EarthLat.Backend.Core.BusinessLogic;
using EarthLat.Backend.Core.Models;
using EarthLat.Backend.Core.Extensions;
using EarthLat.Backend.Core.Dtos;
using EarthLat.Backend.Function.Extension;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;
using System.ComponentModel.DataAnnotations;
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
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(UserDto), Description = "The user matching the credentials")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Bad Request response.", Description = "Request could not be processed.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Resource not found.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized access.")]
        public async Task<ActionResult<UserDto>> Authenticate(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "Authenticate")] HttpRequestData request)
        {
            try
            {
                string requestBody = await request.GetRequestBody();
                var credentials = JsonConvert.DeserializeObject<UserCredentials>(requestBody);
                var userDto = await statisticService.AuthenticateAsync(credentials);
                return (userDto == null)
                    ? new UnauthorizedObjectResult("Username or Password not found")
                    : new OkObjectResult(userDto);
            }
            catch (Exception)
            {
                return new NotFoundResult();
            }
        }

        [Function(nameof(GetSendTimesAsync))]
        [OpenApiOperation(operationId: nameof(GetSendTimesAsync), tags: new[] { "Frontend API" }, Summary = "Get the sending times of a station")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<BarChartDto>), Description = "The start- and endtime(s) of a/all stations sending activity on a certain day")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Bad Request response.", Description = "Request could not be processed.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Resource not found.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized access.")]
        public async Task<ActionResult<BarChartDto>> GetSendTimesAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "SendTimes")] HttpRequestData request)
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
                var (referenceDate, timezoneOffset) = request.Headers.GetHeaders();
                var sendTimes = await statisticService.GetSendTimesAsync(
                    validator,
                    referenceDate,
                    int.Parse(timezoneOffset));
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
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<LineChartDto>), Description = "The average temperature of the uploaded images of a/all station(s) per hour")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Bad Request response.", Description = "Request could not be processed.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Resource not found.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized access.")]
        public async Task<ActionResult<LineChartDto>> GetAverageTemperaturePerHourAsync(
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
                var (referenceDate, timezoneOffset) = request.Headers.GetHeaders();
                var brightnessValues = await statisticService.GetTemperatrueValuesPerHourAsync(
                    validator,
                    referenceDate,
                    int.Parse(timezoneOffset));
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
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<LineChartDto>), Description = "The uploaded images per hour of a/all station(s) on a certain day")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Bad Request response.", Description = "Request could not be processed.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Resource not found.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized access.")]
        public async Task<ActionResult<LineChartDto>> GetImagesPerHourAsync(
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
                var (referenceDate, timezoneOffset) = request.Headers.GetHeaders();
                var sendTimes = await statisticService.GetImagesPerHourAsync(
                    validator,
                    referenceDate,
                    int.Parse(timezoneOffset));
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
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(List<LineChartDto>), Description = "The average brightness of the uploaded images of a/all station(s) per hour")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Bad Request response.", Description = "Request could not be processed.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Resource not found.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized access.")]
        public async Task<ActionResult<LineChartDto>> GetAverageBrightnessPerHourAsync(
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
                var (referenceDate, timezoneOffset) = request.Headers.GetHeaders();
                var brightnessValues = await statisticService.GetBrightnessValuesPerHourAsync(
                    validator,
                    referenceDate,
                    int.Parse(timezoneOffset));
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
