using EarthLat.Backend.Core;
using Microsoft.Azure.Functions.Worker.Configuration;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace EarthLat.Backend.Management.Function
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