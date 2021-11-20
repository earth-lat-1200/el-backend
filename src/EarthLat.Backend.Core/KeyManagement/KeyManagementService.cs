using Newtonsoft.Json;

namespace EarthLat.Backend.Core.KeyManagement
{
    public class KeyManagementService
    {
        private Dictionary<string, string> _keymap;
        private readonly HttpClient _httpClient;
        private readonly string _key;
        private readonly string _url;

        public KeyManagementService(HttpClient httpClient, string functionKey, string functionUrl)
        {
            if (string.IsNullOrWhiteSpace(functionKey))
            {
                throw new ArgumentException($"'{nameof(functionKey)}' cannot be null or empty.", nameof(functionKey));
            }

            if (string.IsNullOrWhiteSpace(functionUrl))
            {
                throw new ArgumentException($"'{nameof(functionUrl)}' cannot be null or whitespace.", nameof(functionUrl));
            }

            _keymap = new Dictionary<string, string>();
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _key = functionKey;
            _url = functionUrl;
        }

        private async Task UpdateKeys()
        {
            _httpClient.DefaultRequestHeaders.Add("x-functions-key", _key);
            var response = await _httpClient.GetAsync(_url);
            var functionKeysDto = JsonConvert.DeserializeObject<FunctionKeysResponseDto>(await response.Content.ReadAsStringAsync());

            if (functionKeysDto is not null && functionKeysDto.Keys?.Count > 0)
            {
                _keymap = functionKeysDto.Keys
                    .Where(fk => fk.Value is not null)
                    .Where(fk => fk.Name is not null)
                    .ToDictionary(fk => fk.Value ?? "", fk => fk.Name ?? "");
            }
        }

        public async Task<string> ValidateKey(string key)
        {
            if (!_keymap.ContainsKey(key))
            {
                await UpdateKeys();
                if (!_keymap.ContainsKey(key))
                {
                    throw new AccessViolationException();
                }
            }
            return _keymap[key];
        }

        private class FunctionKeysResponseDto
        {
            public List<FunctionKeyDto>? Keys { get; set; }
        }

        private class FunctionKeyDto
        {
            public string? Name { get; set; }
            public string? Value { get; set; }
        }
    }
}
