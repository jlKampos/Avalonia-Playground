using OmniWatch.Core.Helpers;
using OmniWatch.Core.Interfaces;
using OmniWatch.Integrations.Contracts.OpenSky;
using OmniWatch.Integrations.Enums;
using OmniWatch.Integrations.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;

namespace OmniWatch.Integrations.Services
{
    public class OpenSkyService : IOpenSkyService
    {
        private readonly IHttpClientFactory _factory;
        private readonly IOpenSkyTokenManager _tokenManager;
        private readonly ISettingsService _settingsService;

        public OpenSkyService(
            IHttpClientFactory factory,
            IOpenSkyTokenManager tokenManager,
            ISettingsService settingsService)
        {
            _factory = factory;
            _tokenManager = tokenManager;
            _settingsService = settingsService;
        }

        public async Task<(OpenSkyRawResponse? Data, RateLimitInfo? RateLimit)> GetAllFlightStatesAsync()
        {
            try
            {
                var settings = _settingsService.Load();
                var client = _factory.CreateClient(ApiType.OpenSky.ToString());

                // PUBLIC MODE
                if (!settings.UseOpenSkyCredentials)
                {
                    client.DefaultRequestHeaders.Authorization = null;

                    var response = await client.GetAsync("states/all");

                    if (!response.IsSuccessStatusCode)
                    {
                        var message = HttpErrorMessages.GetMessage(response.StatusCode);
                        throw new ApiException(response.StatusCode, message);

                    }
                    var json = await response.Content.ReadAsStringAsync();

                    var raw = JsonSerializer.Deserialize<OpenSkyRawResponse>(json);

                    return (raw, null);
                }

                // AUTHENTICATED MODE
                var token = await _tokenManager.GetTokenAsync();

                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                client.DefaultRequestHeaders.UserAgent.ParseAdd("OmniWatch/1.0");

                var authResponse = await client.GetAsync("states/all");

                if (!authResponse.IsSuccessStatusCode)
                {
                    var message = HttpErrorMessages.GetMessage(authResponse.StatusCode);
                    throw new ApiException(authResponse.StatusCode, message);
                }

                var jsonAuth = await authResponse.Content.ReadAsStringAsync();

                var rawAuth = JsonSerializer.Deserialize<OpenSkyRawResponse>(jsonAuth);

                var rate = new RateLimitInfo();

                // Remaining — único header garantido
                if (authResponse.Headers.TryGetValues("X-Rate-Limit-Remaining", out var remaining))
                    rate.Remaining = int.Parse(remaining.First());

                // Limit — inferido pelo role do token
                var role = await _tokenManager.GetRoleAsync(); // método simples que extrai o claim "roles"
                rate.Limit = OpenSkyRateLimitTable.GetDailyLimit(role);

                // Reset — meia-noite UTC
                rate.ResetAt = OpenSkyRateLimitTable.GetDailyResetUtc();

                return (rawAuth, rate);
            }
            catch (Exception ex)
            {

                throw new ApiException("Failed to load flights", ex);

            }
        }


    }
}
