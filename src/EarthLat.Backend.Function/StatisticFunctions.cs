using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using EarthLat.Backend.Core.BusinessLogic;
using EarthLat.Backend.Core.Models;
using EarthLat.Backend.Function.Dtos;
using EarthLat.Backend.Function.Extension;
using EarthLat.Backend.Function.JWT;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace EarthLat.Backend.Function
{
    public class StatisticFunctions
    {
        private readonly StatisticService statisticService;
        private readonly JwtValidator validator;
        private readonly JwtGenerator generator;
        public StatisticFunctions(StatisticService statisticService, JwtValidator validator, JwtGenerator generator)
        {
            this.statisticService = statisticService;
            this.validator = validator;
            this.generator = generator;
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
                var user = statisticService.Authenticate(credentials).Result;
                return (user == null)
                    ? new UnauthorizedObjectResult("Username or Password not found")
                    : new OkObjectResult(new UserDto
                    {
                        Name = user.Name,
                        Privilege = user.Privilege,
                        Token = generator.GenerateJWT(user)
                    });
            }
            catch (Exception)
            {
                return new NotFoundResult();
            }
        }


        [Function(nameof(GetSendTimes))]
        [OpenApiRequestBody("applicaton/json", typeof(UserCredentials), Description = "Contains username and password of the user who attempts to login")]
        [OpenApiOperation(operationId: nameof(GetSendTimes), tags: new[] { "Frontend API" }, Summary = "Authenticate the user")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(UserDto), Description = "The start- and endtime of a stations sending activity on a certain day")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Summary = "Bad Request response.", Description = "Request could not be processed.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.NotFound, Description = "Resource not found.")]
        [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = "Unauthorized access.")]
        public async Task<IActionResult> GetSendTimes(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "SendTimes")] HttpRequestData request)
        {
            try
            {
                string requestBody = await request.GetRequestBody();
                validator.Validate(request);
                if (!validator.IsValid)
                {
                    return new UnauthorizedResult();
                }
                return new OkObjectResult("nice");
            }
            catch (Exception e) { return new ConflictObjectResult(e.Message); }
        }
    }
}
