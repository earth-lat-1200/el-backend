using EarthLat.Backend.Core.BusinessLogic;
using EarthLat.Backend.Core.Exceptions;
using EarthLat.Backend.Core.Interfaces;
using EarthLat.Backend.Core.KeyManagement;
using EarthLat.Backend.Core.TableStorage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace EarthLat.Backend.Core
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEarthLatBackendCore(this IServiceCollection services, string tableStorageConnection, string functionKey, string functionUrl)
        {
            if (string.IsNullOrWhiteSpace(tableStorageConnection))
            {
                throw new ConfigurationException($"'{nameof(tableStorageConnection)}' cannot be null or whitespace.");
            }

            if (string.IsNullOrWhiteSpace(functionKey))
            {
                throw new ConfigurationException($"'{nameof(functionKey)}' cannot be null or whitespace.");
            }

            if (string.IsNullOrWhiteSpace(functionUrl))
            {
                throw new ConfigurationException($"'{nameof(functionUrl)}' cannot be null or whitespace.");
            }

            services.AddSingleton<ISundialLogic, SundialLogic>();
            services.AddSingleton<ITableStorageService>(new TableStorageService(tableStorageConnection));

            services.AddHttpClient();
            services.AddSingleton<IAdminLogic, AdminLogic>();
            var provider = services.BuildServiceProvider();

            services.AddSingleton(new KeyManagementService(provider.GetRequiredService<HttpClient>(), functionKey, functionUrl));
            services.AddSingleton<ITableStorageService>(new TableStorageService(tableStorageConnection));

            return services;
        }
    }
}
