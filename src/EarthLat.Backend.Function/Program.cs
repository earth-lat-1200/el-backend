using Microsoft.Azure.Functions.Worker.Configuration;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.Extensions.Logging;
using EarthLat.Backend.Core;
using EarthLat.Backend.Function.Extension;
using System.IO;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Options;

namespace EarthLat.Backend.Function
{
    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults(worker =>
                    {
                        worker.UseNewtonsoftJson();
                    }
                )
                .ConfigureOpenApi()
                .ConfigureServices(s =>
                {
                    s.AddEarthLatBackendCore(Environment.GetEnvironmentVariable("TABLE_STORAGE_CONNECTION"),
                        Environment.GetEnvironmentVariable("FUNCTIONS_KEY"),
                        Environment.GetEnvironmentVariable("FUNCTION_URL"));
                    s.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
                    s.AddLogging(c => c.AddConsole());
                    s.AddValidation();
                    s.AddCors();
                    //s.AddSingleton<JwtGenerator>();
                    //s.AddSingleton<JwtValidator>();
                })
                .Build();
            host.Run();
        }
    }

}
