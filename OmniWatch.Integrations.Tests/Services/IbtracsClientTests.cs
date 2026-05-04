using Moq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using OmniWatch.Integrations.Services;
using OmniWatch.Integrations.Interfaces;
using OmniWatch.Integrations.Tests.Handlers;
using Xunit;
using System;
using System.IO;

namespace OmniWatch.Integrations.Tests.Services
{
    public class IbtracsClientTests
    {
        private static HttpClient CreateClient(FakeHttpMessageHandler handler)
        {
            return new HttpClient(handler)
            {
                BaseAddress = new Uri("https://test.local/")
            };
        }

        // =========================================================
        // STREAM SUCCESS
        // =========================================================
        [Fact]
        public async Task GetRemoteStreamAsync_ShouldReturnStreamAndLastModified()
        {
            // Arrange
            var handler = new FakeHttpMessageHandler(_ =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StreamContent(new MemoryStream(new byte[] { 1, 2, 3 }))
                };

                response.Content.Headers.LastModified = DateTimeOffset.UtcNow;

                return response;
            });

            var client = CreateClient(handler);
            var sut = new IbtracsClient(client);

            // Act
            var (stream, lastModified) = await sut.GetRemoteStreamAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(stream);
            Assert.True(stream.CanRead);
            Assert.True(lastModified <= DateTimeOffset.UtcNow);
        }
        // =========================================================
        // STREAM HTTP ERROR
        // =========================================================
        [Fact]
        public async Task GetRemoteStreamAsync_ShouldThrow_OnHttpFailure()
        {
            // Arrange
            var handler = new FakeHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.InternalServerError));

            var client = CreateClient(handler);

            var sut = new IbtracsClient(client);

            // Act + Assert
            await Assert.ThrowsAsync<HttpRequestException>(() =>
                sut.GetRemoteStreamAsync(CancellationToken.None));
        }

        // =========================================================
        // LAST MODIFIED (HEAD)
        // =========================================================
        [Fact]
        public async Task GetRemoteLastModifiedAsync_ShouldReturnDate()
        {
            var handler = new FakeHttpMessageHandler(request =>
            {
                if (request.Method == HttpMethod.Head)
                {
                    var response = new HttpResponseMessage(HttpStatusCode.OK);
                    response.Content = new StringContent(string.Empty);
                    response.Content.Headers.LastModified = DateTimeOffset.UtcNow;
                    return response;
                }

                return new HttpResponseMessage(HttpStatusCode.OK);
            });

            var client = CreateClient(handler);

            var sut = new IbtracsClient(client);

            var result = await sut.GetRemoteLastModifiedAsync(CancellationToken.None);

            Assert.NotNull(result);
        }

        // =========================================================
        // LAST MODIFIED FAILURE (returns null)
        // =========================================================
        [Fact]
        public async Task GetRemoteLastModifiedAsync_ShouldReturnNull_OnException()
        {
            var handler = new FakeHttpMessageHandler(_ =>
                throw new HttpRequestException("network error"));

            var client = CreateClient(handler);

            var sut = new IbtracsClient(client);

            var result = await sut.GetRemoteLastModifiedAsync(CancellationToken.None);

            Assert.Null(result);
        }
    }
}