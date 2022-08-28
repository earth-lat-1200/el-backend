using JWT.Algorithms;
using JWT.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthLat.Backend.Core.JWT
{
    public class JwtValidator
    {
        public bool IsValid { get; internal set; }
        public string Station { get; internal set; }
        public int Privilege { get; internal set; }
        public void Validate(HttpRequestData request)
        {
            if (!request.Headers.Contains("Authorization"))
            {
                IsValid = false;
                return;
            }
            string authorizationHeader = request.Headers.GetValues("Authorization").FirstOrDefault();
            if (string.IsNullOrEmpty(authorizationHeader))
            {
                IsValid = false;
                return;
            }
            IDictionary<string, object> claims = null;
            try
            {
                if (authorizationHeader.StartsWith("Bearer"))
                {
                    authorizationHeader = authorizationHeader.Substring(7);
                }
                claims = new JwtBuilder()
                    .WithAlgorithm(new HMACSHA256Algorithm())
                    .WithSecret(Environment.GetEnvironmentVariable("JWT_KEY"))
                    .MustVerifySignature()
                    .Decode<IDictionary<string, object>>(authorizationHeader);
            }
            catch (Exception)
            {
                IsValid = false;
                return;
            }
            if (!claims.ContainsKey("station") || !claims.ContainsKey("privilege"))
            {
                IsValid = false;
                return;
            }
            IsValid = true;
            Station = Convert.ToString(claims["station"]);
            Privilege = int.Parse(Convert.ToString(claims["privilege"]));
        }
    }
}