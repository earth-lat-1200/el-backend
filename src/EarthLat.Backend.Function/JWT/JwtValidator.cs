using JWT.Algorithms;
using JWT.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthLat.Backend.Function.JWT
{
    public class JwtValidator
    {
        public bool IsValid { get; }
        public string Id { get; }
        public string Name { get; }
        public int Privilege { get; }
        public JwtValidator(HttpRequestData request)
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
                    .WithSecret("0b4e7d36c3f96e873f7f9aadcda4c7b2fd1c9e02ca480e7099d6fc7f2ed13f26")
                    .MustVerifySignature()
                    .Decode<IDictionary<string, object>>(authorizationHeader);
            }
            catch (Exception)
            {
                IsValid = false;
                return;
            }
            if (!claims.ContainsKey("id") || !claims.ContainsKey("name") || !claims.ContainsKey("privilege"))
            {
                IsValid = false;
                return;
            }
            IsValid = true;
            Name = Convert.ToString(claims["name"]);
            Id = Convert.ToString(claims["id"]);
            Privilege = int.Parse(Convert.ToString(claims["privilege"]));
        }
    }
}