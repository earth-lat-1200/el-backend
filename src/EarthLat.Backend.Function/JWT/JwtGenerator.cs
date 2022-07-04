using EarthLat.Backend.Core.Models;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarthLat.Backend.Function.JWT
{
    public class JwtGenerator
    {
        private readonly IJwtAlgorithm _algorithm;
        private readonly IJsonSerializer _serializer;
        private readonly IBase64UrlEncoder _base64Encoder;
        private readonly IJwtEncoder _jwtEncoder;
        public JwtGenerator()
        {
            _algorithm = new HMACSHA256Algorithm();
            _serializer = new JsonNetSerializer();
            _base64Encoder = new JwtBase64UrlEncoder();
            _jwtEncoder = new JwtEncoder(_algorithm, _serializer, _base64Encoder);
        }
        public string GenerateJWT(User user)
        {
            Dictionary<string, object> claims = new Dictionary<string, object> {
                {"id",user.RowKey},
                {"name",user.Name},
                {"privilege",user.Privilege},
            };
            string token = _jwtEncoder.Encode(claims, Environment.GetEnvironmentVariable("JWT_KEY"));
            return token;
        }
    }
}
