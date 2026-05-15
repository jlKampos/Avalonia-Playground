using OmniWatch.Integrations.Contracts.Seismic;
using OmniWatch.Integrations.Enums;
using OmniWatch.Integrations.Exceptions;
using OmniWatch.Integrations.Helpers;
using OmniWatch.Integrations.Interfaces;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

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
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
        };

        /// <summary>
        /// Realiza um pedido GET e desserializa o JSON de resposta.
        /// </summary>
        public async Task<T?> GetAsync<T>(
            string endpoint,
            ApiType type,
            string? bearerToken = null,
            CancellationToken ct = default)
        {
            var client = _factory.CreateClient(type.ToString());

            if (type == ApiType.OpenSky && !string.IsNullOrWhiteSpace(bearerToken))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            }

            var response = await client.GetAsync(endpoint, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return default;

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }

        /// <summary>
        /// Realiza um pedido GET e retorna o Stream de resposta (útil para ficheiros grandes).
        /// </summary>
        public async Task<Stream> GetStreamAsync(string endpoint, ApiType type, CancellationToken ct = default)
        {
            var client = _factory.CreateClient(type.ToString());

            var response = await client.GetAsync(endpoint, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                throw new ApiException($"Failed request: {response.StatusCode}");

            return await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        }
    }
}