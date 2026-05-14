using OmniWatch.Core.Helpers;
using OmniWatch.Core.Interfaces;
using OmniWatch.Integrations.Contracts.OpenSky;
using OmniWatch.Integrations.Enums;
using OmniWatch.Integrations.Exceptions;
using OmniWatch.Integrations.Helpers;
using OmniWatch.Integrations.Interfaces;
using OmniWatch.Integrations.Localization;
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
            return await GetOpenSkyStatesInternalAsync("states/all");
        }

        public async Task<(OpenSkyRawResponse? Data, RateLimitInfo? RateLimit)> GetFlightStatesInViewportAsync(double lamin, double lomin, double lamax, double lomax)
        {
            // Important: Use CultureInfo.InvariantCulture to ensure dots (.) instead of commas (,) in coordinates
            var query = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "states/all?lamin={0}&lomin={1}&lamax={2}&lomax={3}",
                lamin, lomin, lamax, lomax);

            return await GetOpenSkyStatesInternalAsync(query);
        }

        private async Task<(OpenSkyRawResponse? Data, RateLimitInfo? RateLimit)> GetOpenSkyStatesInternalAsync(string endpoint)
        {
            try
            {
                var settings = _settingsService.Load();
                var client = _factory.CreateClient(ApiType.OpenSky.ToString());

                if (settings.UseOpenSkyCredentials)
                {
                    var token = await _tokenManager.GetTokenAsync();
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("OmniWatch/1.0");
                }
                else
                {
                    client.DefaultRequestHeaders.Authorization = null;
                }

                var response = await client.GetAsync(endpoint);

                if (!response.IsSuccessStatusCode)
                {
                    var message = HttpErrorMessages.GetMessage(response.StatusCode);
                    throw new ApiException(response.StatusCode, message);
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // OpenSky returns null or empty for "states" property if no flights are in the area
                var statesList = new List<StateVectorItem>();
                if (root.TryGetProperty("states", out var statesElement) && statesElement.ValueKind == JsonValueKind.Array)
                {
                    statesList = statesElement.EnumerateArray()
                        .Select(x => OpenSkyRawConverter.ConvertRaw(x.EnumerateArray().ToList()))
                        .ToList();
                }

                var result = new OpenSkyRawResponse
                {
                    Time = root.GetProperty("time").GetInt64(),
                    States = statesList
                };

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
            catch (Exception ex) when (!(ex is ApiException))
            {
                throw new ApiException(IL.Translation("OpenSky_FailedToLoad_Flights"), ex);
            }
        }

    }
}
