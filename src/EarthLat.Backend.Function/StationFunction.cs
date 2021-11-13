using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace EarthLat.Backend.Function
{
    public class StationFunction
    {
        private readonly IStationLogic stationLogic;

        public StationFunction(IStationLogic stationLogic)
        {
            this.stationLogic = stationLogic ?? throw new ArgumentNullException(nameof(stationLogic));
        }

        [FunctionName(nameof(GetStations))]
        [OpenApiOperation(operationId: "GetStations", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public static IActionResult GetStations(
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

       




        [FunctionName(nameof(GetStationById))]
        [OpenApiOperation(operationId: "GetStationById", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public static IActionResult GetStationById(
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
        }
    }
}

