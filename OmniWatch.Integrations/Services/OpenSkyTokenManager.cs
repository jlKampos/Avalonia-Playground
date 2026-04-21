using Microsoft.Extensions.Logging;
using OmniWatch.Core.Enums;
using OmniWatch.Core.Helpers;
using OmniWatch.Core.Interfaces;
using OmniWatch.Core.Models;
using OmniWatch.Integrations.Contracts.OpenSky;
using OmniWatch.Integrations.Enums;
using OmniWatch.Integrations.Interfaces;
using System.Net;
using System.Text.Json;

namespace OmniWatch.Integrations.Services
{
    public class OpenSkyTokenManager : IOpenSkyTokenManager
    {
        private readonly IHttpClientFactory _factory;
        private readonly ISettingsService _settings;
        private readonly ISecretService _secret;
        private readonly ILogger<OpenSkyTokenManager> _logger;

        private string? _token;
        private DateTime _expiresAt;

        private static readonly SemaphoreSlim _lock = new(1, 1);

        private const int RefreshMarginSeconds = 30;

        public OpenSkyTokenManager(IHttpClientFactory factory, ISettingsService settings, ISecretService secret, ILogger<OpenSkyTokenManager> logger)
        {
            _factory = factory;
            _settings = settings;
            _secret = secret;
            _logger = logger;
        }

        public async Task<string> GetRoleAsync()
        {
            var token = await GetTokenAsync();

            if (string.IsNullOrWhiteSpace(token))
                return "OPENSKY_API_DEFAULT";

            var payload = JwtHelper.DecodePayload(token);

            if (payload.TryGetProperty("roles", out var rolesElement))
            {
                if (rolesElement.ValueKind == JsonValueKind.Array)
                    return rolesElement[0].GetString() ?? "OPENSKY_API_DEFAULT";

                if (rolesElement.ValueKind == JsonValueKind.String)
                    return rolesElement.GetString() ?? "OPENSKY_API_DEFAULT";
            }

            return "OPENSKY_API_DEFAULT";
        }

        public async Task<string?> GetTokenAsync()
        {
            await _lock.WaitAsync();
            try
            {
                _logger.LogDebug("GetTokenAsync started");

                // 1. memory cache
                if (!string.IsNullOrWhiteSpace(_token) &&
                    DateTime.UtcNow < _expiresAt)
                {
                    _logger.LogDebug("Returning cached token (expires at {ExpiresAt})", _expiresAt);
                    return _token;
                }

                _logger.LogDebug("Memory token invalid or expired");

                // 2. persisted token
                var storedToken = await _secret.GetAsync(
                    SecretKeys.Token(ApiProvider.OpenSky));

                if (!string.IsNullOrWhiteSpace(storedToken))
                {
                    _logger.LogDebug("Found stored token in secrets");

                    if (!JwtHelper.IsExpired(storedToken))
                    {
                        _logger.LogInformation("Using valid persisted OpenSky token");
                        _token = storedToken;
                        return _token;
                    }

                    _logger.LogWarning("Stored token exists but is expired");
                }
                else
                {
                    _logger.LogDebug("No stored token found in secrets");
                }

                // 3. refresh
                _logger.LogInformation("Refreshing OpenSky token");

                var result = await RefreshTokenAsync();

                if (result.Status == OpenSkyAuthStatus.Success)
                {
                    _logger.LogInformation("Token refresh successful");
                }
                else
                {
                    _logger.LogWarning("Token refresh failed with status {Status}", result.Status);
                }

                return result.AccessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OpenSky token");
                return null;
            }
            finally
            {
                _lock.Release();
                _logger.LogDebug("GetTokenAsync finished");
            }
        }

        public async Task<OpenSkyAuthResult> RefreshTokenAsync()
        {
            _logger.LogInformation("Starting token refresh");

            var settings = _settings.Load();

            var apiKey = await _secret.GetAsync(
                SecretKeys.ApiKey(ApiProvider.OpenSky));

            if (!settings.UseOpenSkyCredentials)
                _logger.LogWarning("OpenSky credentials disabled in settings");

            if (string.IsNullOrWhiteSpace(settings.OpenSkyClientId))
                _logger.LogWarning("Missing OpenSkyClientId");

            if (string.IsNullOrWhiteSpace(apiKey))
                _logger.LogWarning("Missing OpenSky API key");

            if (!settings.UseOpenSkyCredentials ||
                string.IsNullOrWhiteSpace(settings.OpenSkyClientId) ||
                string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("OpenSky authentication aborted due to missing config");

                return new OpenSkyAuthResult
                {
                    Status = OpenSkyAuthStatus.Unauthorized
                };
            }

            try
            {
                var client = _factory.CreateClient("OpenSkyAuth");

                _logger.LogDebug("Sending token request to OpenSky API");

                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = settings.OpenSkyClientId,
                    ["client_secret"] = apiKey
                });

                var response = await client.PostAsync(
                    "auth/realms/opensky-network/protocol/openid-connect/token",
                    content
                );

                _logger.LogDebug("OpenSky response: {StatusCode}", response.StatusCode);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("OpenSky returned Unauthorized");
                    return new OpenSkyAuthResult { Status = OpenSkyAuthStatus.Unauthorized };
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("OpenSky token request failed with status {StatusCode}", response.StatusCode);
                    return new OpenSkyAuthResult { Status = OpenSkyAuthStatus.Error };
                }

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<OpenSkyTokenResponse>(json);

                _token = data?.AccessToken;

                var expiresIn = data?.ExpiresIn ?? 1800;

                _expiresAt = DateTime.UtcNow
                    .AddSeconds(expiresIn - RefreshMarginSeconds);

                _logger.LogInformation(
                    "Token refreshed successfully. Expires in {ExpiresIn}s (at {ExpiresAt})",
                    expiresIn,
                    _expiresAt);

                // persist token
                if (!string.IsNullOrWhiteSpace(_token))
                {
                    await _secret.SetAsync(
                        SecretKeys.Token(ApiProvider.OpenSky),
                        _token);

                    _logger.LogDebug("Token persisted to secret storage");
                }

                return new OpenSkyAuthResult
                {
                    Status = OpenSkyAuthStatus.Success,
                    AccessToken = _token
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during token refresh");
                return new OpenSkyAuthResult
                {
                    Status = OpenSkyAuthStatus.Error
                };
            }
        }

        public async Task<OpenSkyAuthResult> TestCredentialsAsync(string clientId, string clientSecret)
        {
            _logger.LogInformation("Testing OpenSky credentials");

            try
            {
                var client = _factory.CreateClient("OpenSkyAuth");

                var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = clientId,
                    ["client_secret"] = clientSecret
                });

                var response = await client.PostAsync(
                    "auth/realms/opensky-network/protocol/openid-connect/token",
                    content
                );

                _logger.LogDebug("Test credentials response: {StatusCode}", response.StatusCode);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("Test credentials failed: Unauthorized");
                    return new OpenSkyAuthResult { Status = OpenSkyAuthStatus.Unauthorized };
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Test credentials failed: {StatusCode}", response.StatusCode);
                    return new OpenSkyAuthResult { Status = OpenSkyAuthStatus.Error };
                }

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<OpenSkyTokenResponse>(json);

                _logger.LogInformation("Test credentials successful");

                return new OpenSkyAuthResult
                {
                    Status = OpenSkyAuthStatus.Success,
                    AccessToken = data?.AccessToken
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing OpenSky credentials");

                return new OpenSkyAuthResult
                {
                    Status = OpenSkyAuthStatus.Error
                };
            }
        }
    }
}