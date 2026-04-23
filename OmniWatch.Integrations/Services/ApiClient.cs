using OmniWatch.Integrations.Enums;
using OmniWatch.Integrations.Exceptions;
using OmniWatch.Integrations.Interfaces;
using System.Net.Http.Headers;
using System.Text.Json;

namespace OmniWatch.Integrations.Services
{
    public class ApiClient : IApiClient
    {
        private readonly IHttpClientFactory _factory;

        public ApiClient(IHttpClientFactory factory)
        {
            _factory = factory;
        }

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public async Task<T?> GetAsync<T>(string endpoint, ApiType type, string? bearerToken = null)
        {
            var client = _factory.CreateClient(type.ToString());

            if (type == ApiType.OpenSky && !string.IsNullOrWhiteSpace(bearerToken))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            }

            var response = await client.GetAsync(endpoint);

            if (!response.IsSuccessStatusCode)
                return default;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
    }
}
