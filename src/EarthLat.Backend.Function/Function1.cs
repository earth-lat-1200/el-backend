using System.Collections.Generic;
using System.IO;
using System.Linq;
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

namespace EarthLat.Backend.Function
{
    public static class Function1
    {
        public static readonly List<Station> Stations = new List<Station>();

        [FunctionName(nameof(GetAllStations))]
        [OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public static IActionResult GetAllStations(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "station")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Getting Station list items");

            return new OkObjectResult(Stations);
        }

        [FunctionName(nameof(GetStationById))]
        [OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public static IActionResult GetStationById(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "station/{id}")] HttpRequest req,
            ILogger log, string id)
        {
            log.LogInformation("Getting Station by id");

            var station = Stations.FirstOrDefault(t => t.Id == id);
            if(station == null)
            {
                return new NotFoundResult();
            }
            return new OkObjectResult(station);
        }
    }
} 

