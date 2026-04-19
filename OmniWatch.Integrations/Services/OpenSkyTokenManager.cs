using OmniWatch.Core.Helpers;
using OmniWatch.Core.Interfaces;
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

        private string? _token;
        private DateTime _expiresAt;

        private const int RefreshMarginSeconds = 30;

        public OpenSkyTokenManager(
            IHttpClientFactory factory,
            ISettingsService settings,
            ISecretService secret)
        {
            _factory = factory;
            _settings = settings;
            _secret = secret;
        }

        public async Task<string> GetRoleAsync()
        {
            var token = await GetTokenAsync();

            if (string.IsNullOrWhiteSpace(token))
                return "OPENSKY_API_DEFAULT"; // fallback seguro

            var payload = JwtHelper.DecodePayload(token);

            // roles pode ser array ou string
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
            if (!string.IsNullOrWhiteSpace(_token) && DateTime.UtcNow < _expiresAt)
                return _token;

            var result = await RefreshTokenAsync();
            return result.AccessToken;
        }

        public async Task<OpenSkyAuthResult> RefreshTokenAsync()
        {
            var settings = _settings.Load();
            var clientSecret = _secret.Load();

            if (!settings.UseOpenSkyCredentials ||
                string.IsNullOrWhiteSpace(settings.OpenSkyClientId) ||
                string.IsNullOrWhiteSpace(clientSecret))
            {
                return new OpenSkyAuthResult
                {
                    Status = OpenSkyAuthStatus.Unauthorized
                };
            }

            var client = _factory.CreateClient("OpenSkyAuth");

            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = settings.OpenSkyClientId,
                ["client_secret"] = clientSecret
            });

            var response = await client.PostAsync(
                "auth/realms/opensky-network/protocol/openid-connect/token",
                content
            );

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return new OpenSkyAuthResult
                {
                    Status = OpenSkyAuthStatus.Unauthorized
                };
            }

            if (!response.IsSuccessStatusCode)
            {
                return new OpenSkyAuthResult
                {
                    Status = OpenSkyAuthStatus.Error
                };
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<OpenSkyTokenResponse>(json);

            _token = data?.AccessToken;
            var expiresIn = data?.ExpiresIn ?? 1800;

            _expiresAt = DateTime.UtcNow.AddSeconds(expiresIn - RefreshMarginSeconds);

            return new OpenSkyAuthResult
            {
                Status = OpenSkyAuthStatus.Success,
                AccessToken = _token
            };
        }

        public async Task<OpenSkyAuthResult> TestCredentialsAsync(string clientId, string clientSecret)
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

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return new OpenSkyAuthResult { Status = OpenSkyAuthStatus.Unauthorized };

            if (!response.IsSuccessStatusCode)
                return new OpenSkyAuthResult { Status = OpenSkyAuthStatus.Error };

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<OpenSkyTokenResponse>(json);

            return new OpenSkyAuthResult
            {
                Status = OpenSkyAuthStatus.Success,
                AccessToken = data?.AccessToken
            };
        }

    }
}
