using EarthLat.Backend.Function.Validation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
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
    }
}
