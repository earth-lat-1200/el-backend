using EarthLat.Backend.Function.Validation;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthLat.Backend.Function.Extension
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddValidation(this IServiceCollection services)
        {
            services.AddSingleton<IWebCamContentDtoValidator, WebCamContentDtoValidator>();

            return services;
        }
        public async static Task<string> GetRequestBody(this HttpRequestData request)
        {
            StreamReader requestReader = new StreamReader(request.Body);
            string requestBody = await requestReader.ReadToEndAsync();
            return requestBody;
        }
    }
}
