﻿using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace EarthLat.Backend.Function
{
    public static class HttpRequestDataExtensions
    {
        public static string GetHeaderKey(this HttpRequestData request)
        {
            try
            {
                var context = JsonConvert.DeserializeObject<Dictionary<string, string>>((string)request.FunctionContext.BindingContext.BindingData["Headers"]);
                return context["Authorization"];
            }
            catch (System.Exception)
            {
                return null;
            }

        }
    }
}
