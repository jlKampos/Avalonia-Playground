using OmniWatch.Core.Helpers;
using OmniWatch.Core.Interfaces;
using OmniWatch.Integrations.Contracts.OpenSky;
using OmniWatch.Integrations.Enums;
using OmniWatch.Integrations.Interfaces;
using System.Text.Json;
using OmniWatch.Integrations.Helpers;
using OmniWatch.Integrations.Exceptions;
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

                HttpResponseMessage response;

                // =========================
                // PUBLIC MODE
                // =========================
                if (!settings.UseOpenSkyCredentials)
                {
                    client.DefaultRequestHeaders.Authorization = null;
                    response = await client.GetAsync("states/all");
                }
                else
                {
                    // =========================
                    // AUTH MODE
                    // =========================
                    var token = await _tokenManager.GetTokenAsync();

                    client.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                    client.DefaultRequestHeaders.UserAgent.ParseAdd("OmniWatch/1.0");

                    response = await client.GetAsync("states/all");
                }

                if (!response.IsSuccessStatusCode)
                {
                    var message = HttpErrorMessages.GetMessage(response.StatusCode);
                    throw new ApiException(response.StatusCode, message);
                }

                var json = await response.Content.ReadAsStringAsync();

                // =========================
                // MANUAL PARSING (CORRECTO PARA OPEN SKY)
                // =========================
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var result = new OpenSkyRawResponse
                {
                    Time = root.GetProperty("time").GetInt64(),
                    States = root.GetProperty("states")
                        .EnumerateArray()
                        .Select(x => OpenSkyRawConverter.ConvertRaw(
                            x.EnumerateArray().ToList()
                        ))
                        .ToList()
                };

                // =========================
                // RATE LIMIT
                // =========================
                RateLimitInfo? rate = null;

                if (settings.UseOpenSkyCredentials)
                {
                    rate = new RateLimitInfo();

                    if (response.Headers.TryGetValues("X-Rate-Limit-Remaining", out var remaining))
                        rate.Remaining = int.Parse(remaining.First());

                    var role = await _tokenManager.GetRoleAsync();
                    rate.Limit = OpenSkyRateLimitTable.GetDailyLimit(role);
                    rate.ResetAt = OpenSkyRateLimitTable.GetDailyResetUtc();
                }

                return (result, rate);
            }
            catch (Exception ex)
            {
                throw new ApiException("Failed to load flights", ex);
            }
        }

    }
}
