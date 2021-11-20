using EarthLat.Backend.Core;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

[assembly: FunctionsStartup(typeof(EarthLat.Backend.Function.Startup))]

namespace EarthLat.Backend.Function
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddEarthLatBackendCore(Environment.GetEnvironmentVariable("TABLE_STORAGE_CONNECTION"));
            builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            builder.Services.AddLogging(c => c.AddConsole());
        }
    }
}