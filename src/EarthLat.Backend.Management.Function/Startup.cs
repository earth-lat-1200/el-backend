using EarthLat.Backend.Core;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

[assembly: FunctionsStartup(typeof(EarthLat.Backend.Management.Function.Startup))]
namespace EarthLat.Backend.Management.Function
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddEarthLatBackendAdminCore(Environment.GetEnvironmentVariable("TABLE_STORAGE_CONNECTION"), Environment.GetEnvironmentVariable("FUNCTIONS_KEY"), Environment.GetEnvironmentVariable("FUNCTION_URL"));
            builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            builder.Services.AddLogging(c => c.AddConsole());
        }
    }
}
