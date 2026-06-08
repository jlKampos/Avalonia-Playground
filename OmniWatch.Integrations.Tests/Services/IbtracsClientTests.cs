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
            var expectedDate = DateTimeOffset.UtcNow;

            var handler = new FakeHttpMessageHandler(request =>
            {
                // 1. Check if the client is looking for the version list (HTML discovery)
                // Adjust the URL snippet to match what your client is actually calling
                if (!request.RequestUri!.AbsolutePath.EndsWith(".csv"))
                {
                    var htmlDiscoveryResponse = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        // Provide a mock HTML that contains a version-like link
                        // Your IbtracsClient discovery logic likely looks for a folder pattern like 'v04r01'
                        Content = new StringContent("<a href=\"v04r01/\">v04r01/</a>")
                    };
                    return htmlDiscoveryResponse;
                }

                // 2. Return the actual CSV stream for the second request
                var csvResponse = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StreamContent(new MemoryStream(new byte[] { 1, 2, 3 }))
                };

                csvResponse.Content.Headers.LastModified = expectedDate;
                return csvResponse;
            });

            var client = CreateClient(handler);
            var sut = new IbtracsClient(client);

            // Act
            var (stream, lastModified) = await sut.GetRemoteStreamAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(stream);
            Assert.True(stream.CanRead);
            // Use .Date or a tolerance comparison to avoid sub-millisecond failures
            Assert.Equal(expectedDate.Date, lastModified.Date);
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
            // 1. Setup a clean date
            var expectedDate = new DateTimeOffset(2023, 10, 27, 12, 0, 0, TimeSpan.Zero);

            var handler = new FakeHttpMessageHandler(request =>
            {
                // STAGE 1: The Discovery Call 
                // If the URL doesn't end in .csv, the client is probably scraping for the version folder
                if (!request.RequestUri!.AbsolutePath.EndsWith(".csv"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        // Return HTML that your ResolveUrlAsync logic can parse
                        Content = new StringContent("<a href=\"v04r01/\">v04r01/</a>")
                    };
                }

                // STAGE 2: The actual HEAD call for the CSV file
                var response = new HttpResponseMessage(HttpStatusCode.OK);

                // HttpClient requires Content to be set to hold 'Last-Modified' headers
                response.Content = new ByteArrayContent(Array.Empty<byte>());
                response.Content.Headers.LastModified = expectedDate;

                return response;
            });

            var client = CreateClient(handler);
            var sut = new IbtracsClient(client);

            // Act
            var result = await sut.GetRemoteLastModifiedAsync(CancellationToken.None);

            // Assert
            Assert.True(result.HasValue, "The LastModified date should not be null");
            Assert.Equal(expectedDate, result.Value);
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