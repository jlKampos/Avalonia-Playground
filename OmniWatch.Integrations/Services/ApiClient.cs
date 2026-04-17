using OmniWatch.Integrations.Enums;
using OmniWatch.Integrations.Exceptions;
using OmniWatch.Integrations.Interfaces;
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

        public async Task<T> GetAsync<T>(string endpoint, ApiType api)
        {
            try
            {
                var client = _factory.CreateClient(api.ToString());

                var response = await client.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<T>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            }
            catch (Exception ex)
            {
                throw new ApiException($"API Error calling '{api}/{endpoint}'", ex);
            }
        }
    }
}
