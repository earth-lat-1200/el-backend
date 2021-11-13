using EarthLat.Backend.Core.Abstraction;
using EarthLat.Backend.Core.FileStorage;
using EarthLat.Backend.Core.TableStorage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


[assembly: FunctionsStartup(typeof(EarthLat.Backend.Function.Startup))]

namespace EarthLat.Backend.Function
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient();

            builder.Services.AddSingleton<IFileStorage,FileStorageService>();
            builder.Services.AddSingleton<ITableStorageManagement,TableStorageManagement>();
            
            builder.Services.AddLogging();
        }

    }
}