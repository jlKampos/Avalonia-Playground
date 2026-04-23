using Microsoft.Extensions.Logging;
using Moq;
using OmniWatch.Core.Enums;
using OmniWatch.Core.Helpers;
using OmniWatch.Core.Interfaces;
using OmniWatch.Core.Settings;
using OmniWatch.Integrations.Enums;
using OmniWatch.Integrations.Services;
using OmniWatch.Integrations.Tests.Handlers;
using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace OmniWatch.Integrations.Tests.Services
{
    public class OpenSkyTokenManagerTests
    {
        private readonly Mock<IHttpClientFactory> _factoryMock;
        private readonly Mock<ISettingsService> _settingsMock;
        private readonly Mock<ISecretService> _secretMock;
        private readonly Mock<ILogger<OpenSkyTokenManager>> _loggerMock;

        public OpenSkyTokenManagerTests()
        {
            _factoryMock = new Mock<IHttpClientFactory>();
            _settingsMock = new Mock<ISettingsService>();
            _secretMock = new Mock<ISecretService>();
            _loggerMock = new Mock<ILogger<OpenSkyTokenManager>>();
        }

        private OpenSkyTokenManager CreateSut(HttpClient client)
        {
            _factoryMock
                .Setup(f => f.CreateClient("OpenSkyAuth"))
                .Returns(client);

            return new OpenSkyTokenManager(
                _factoryMock.Object,
                _settingsMock.Object,
                _secretMock.Object,
                _loggerMock.Object);
        }

        private static void ResetState(OpenSkyTokenManager sut)
        {
            typeof(OpenSkyTokenManager)
                .GetField("_token", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(sut, null);

            typeof(OpenSkyTokenManager)
                .GetField("_expiresAt", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(sut, DateTime.MinValue);
        }

        // =========================
        // CACHE EM MEMÓRIA
        // =========================
        [Fact]
        public async Task GetTokenAsync_Should_Return_Cached_Token()
        {
            var handler = new FakeHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK));

            var client = new HttpClient(handler);

            var sut = CreateSut(client);

            typeof(OpenSkyTokenManager)
                .GetField("_token", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(sut, "cached-token");

            typeof(OpenSkyTokenManager)
                .GetField("_expiresAt", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(sut, DateTime.UtcNow.AddMinutes(10));

            var result = await sut.GetTokenAsync();

            Assert.Equal("cached-token", result);
        }

        // =========================
        // TOKEN PERSISTIDO
        // =========================
        [Fact]
        public async Task GetTokenAsync_Should_Use_Stored_Token_If_Valid()
        {
            _secretMock
                .Setup(s => s.GetAsync(SecretKeys.Token(ApiProvider.OpenSky)))
                .ReturnsAsync("header.eyJleHAiOjk5OTk5OTk5OTl9.signature"); // exp futuro

            var handler = new FakeHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK));

            var client = new HttpClient(handler);

            var sut = CreateSut(client);

            ResetState(sut);

            var result = await sut.GetTokenAsync();

            Assert.Equal("header.eyJleHAiOjk5OTk5OTk5OTl9.signature", result);
        }

        // =========================
        // REFRESH TOKEN
        // =========================
        [Fact]
        public async Task GetTokenAsync_Should_Refresh_When_Expired()
        {
            _settingsMock
                .Setup(s => s.Load())
                .Returns(new AppSettings
                {
                    UseOpenSkyCredentials = true,
                    OpenSkyClientId = "test-client"
                });

            _secretMock
                .Setup(s => s.GetAsync(SecretKeys.Token(ApiProvider.OpenSky)))
                .ReturnsAsync("header.eyJleHAiOjB9.signature");

            _secretMock
                .Setup(s => s.GetAsync(SecretKeys.ApiKey(ApiProvider.OpenSky)))
                .ReturnsAsync("test-secret");

            var json = @"{
                ""access_token"": ""new-token"",
                ""expires_in"": 1800
            }";

            var handler = new FakeHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://test.local/")
            };

            var sut = CreateSut(client);

            ResetState(sut);

            var result = await sut.GetTokenAsync();

            Assert.Equal("new-token", result);
        }

        // =========================
        // ERRO HTTP
        // =========================
        [Fact]
        public async Task RefreshTokenAsync_Should_Return_Error_On_Failed_Request()
        {
            _settingsMock
                .Setup(s => s.Load())
                .Returns(new AppSettings
                {
                    UseOpenSkyCredentials = true,
                    OpenSkyClientId = "test-client"
                });

            _secretMock
                .Setup(s => s.GetAsync(SecretKeys.ApiKey(ApiProvider.OpenSky)))
                .ReturnsAsync("test-secret");

            var handler = new FakeHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.BadRequest));

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://test.local/")
            };

            var sut = CreateSut(client);

            var result = await sut.RefreshTokenAsync();

            Assert.Equal(OpenSkyAuthStatus.Error, result.Status);
        }
        // =========================
        // TEST CREDENTIALS OK
        // =========================
        [Fact]
        public async Task TestCredentialsAsync_Should_Return_Success()
        {
            var json = @"{
                ""access_token"": ""test-token"",
                ""expires_in"": 1800
            }";

            var handler = new FakeHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json)
                });

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://test.local/")
            };

            var sut = CreateSut(client);

            var result = await sut.TestCredentialsAsync("id", "secret");

            Assert.Equal(OpenSkyAuthStatus.Success, result.Status);
            Assert.Equal("test-token", result.AccessToken);
        }

        // =========================
        // TEST CREDENTIALS FAIL
        // =========================
        [Fact]
        public async Task TestCredentialsAsync_Should_Return_Unauthorized()
        {
            var handler = new FakeHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.Unauthorized));

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://test.local/")
            };

            _factoryMock
                .Setup(f => f.CreateClient("OpenSkyAuth"))
                .Returns(client);

            var sut = CreateSut(client);

            var result = await sut.TestCredentialsAsync("id", "secret");

            Assert.Equal(OpenSkyAuthStatus.Unauthorized, result.Status);
        }
    }
}