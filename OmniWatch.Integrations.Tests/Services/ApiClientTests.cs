using Moq;
using OmniWatch.Integrations.Enums;
using OmniWatch.Integrations.Services;
using OmniWatch.Integrations.Tests.Handlers;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace OmniWatch.Integrations.Tests.Services
{
    public class ApiClientTests
    {
        private static HttpClient CreateClient(FakeHttpMessageHandler handler)
        {
            return new HttpClient(handler)
            {
                BaseAddress = new System.Uri("https://test.local/")
            };
        }

        private static Mock<IHttpClientFactory> CreateFactory(HttpClient client, ApiType apiType)
        {
            var factory = new Mock<IHttpClientFactory>();

            factory
                .Setup(f => f.CreateClient(apiType.ToString()))
                .Returns(client);

            factory
                .Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(client);

            return factory;
        }

        [Fact]
        public async Task GetAsync_Should_Deserialize_Response()
        {
            var handler = new FakeHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"value\": 123}")
                });

            var client = CreateClient(handler);
            var factory = CreateFactory(client, ApiType.Ipma);

            var api = new ApiClient(factory.Object);

            var result = await api.GetAsync<TestDto>("test", ApiType.Ipma);

            Assert.NotNull(result);
            Assert.Equal(123, result.Value);
        }

        [Fact]
        public async Task GetAsync_Should_Return_Null_On_Http_Error()
        {
            var handler = new FakeHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.BadRequest));

            var client = CreateClient(handler);
            var factory = CreateFactory(client, ApiType.Ipma);

            var api = new ApiClient(factory.Object);

            var result = await api.GetAsync<TestDto>("test", ApiType.Ipma);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAsync_Should_Add_BearerToken_For_OpenSky()
        {
            var handler = new FakeHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}")
                });

            var client = CreateClient(handler);
            var factory = CreateFactory(client, ApiType.OpenSky);

            var api = new ApiClient(factory.Object);

            await api.GetAsync<object>("endpoint", ApiType.OpenSky, "abc123");

            Assert.NotNull(handler.LastRequest);
            Assert.NotNull(handler.LastRequest.Headers.Authorization);
            Assert.Equal("Bearer", handler.LastRequest.Headers.Authorization.Scheme);
            Assert.Equal("abc123", handler.LastRequest.Headers.Authorization.Parameter);
        }

        [Fact]
        public async Task GetAsync_Should_Not_Add_BearerToken_For_Other_Apis()
        {
            var handler = new FakeHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}")
                });

            var client = CreateClient(handler);
            var factory = CreateFactory(client, ApiType.Ipma);

            var api = new ApiClient(factory.Object);

            await api.GetAsync<object>("endpoint", ApiType.Ipma, "abc123");

            Assert.NotNull(handler.LastRequest);
            Assert.Null(handler.LastRequest.Headers.Authorization);
        }

        public class TestDto
        {
            public int Value { get; set; }
        }
    }
}