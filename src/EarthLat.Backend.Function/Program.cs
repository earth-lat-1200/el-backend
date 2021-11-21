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

namespace EarthLat.Backend.Function
{
    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults(worker => worker.UseNewtonsoftJson())
                .ConfigureOpenApi()
                .ConfigureServices(s =>
                {
                    s.AddEarthLatBackendCore(Environment.GetEnvironmentVariable("TABLE_STORAGE_CONNECTION"));
                    s.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
                    s.AddLogging(c => c.AddConsole());

                })
                .Build();

            host.Run();
        }
    }
}