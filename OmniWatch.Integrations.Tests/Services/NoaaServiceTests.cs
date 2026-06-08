using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using OmniWatch.Core.Interfaces;
using OmniWatch.Integrations.Contracts.NOA;
using OmniWatch.Integrations.Contracts.NOA.ActiveStorms;
using OmniWatch.Integrations.Enums;
using OmniWatch.Integrations.Exceptions;
using OmniWatch.Integrations.Interfaces;
using OmniWatch.Integrations.Persistence;
using OmniWatch.Integrations.Services;
using System.Text;
using Xunit;

namespace OmniWatch.Integrations.Tests.Services
{
    public class NoaaServiceTests : IDisposable
    {
        private readonly Mock<IApiClient> _api = new();
        private readonly Mock<IIbtracsClient> _ibtracs = new();
        private readonly Mock<IGlobalProgressService> _progress = new();
        private readonly Mock<ILogger<NoaaService>> _logger = new();

        private readonly SqliteConnection _connection;
        private ServiceProvider _provider;

        public NoaaServiceTests()
        {
            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();
        }

        private NoaaService CreateSut()
        {
            var services = new ServiceCollection();

            services.AddDbContext<NoaaCacheContext>(options =>
                options.UseSqlite(_connection));

            services.AddScoped<NoaaService>();

            services.AddSingleton(_api.Object);
            services.AddSingleton(_ibtracs.Object);
            services.AddSingleton(_progress.Object);
            services.AddSingleton(_logger.Object);

            _provider = services.BuildServiceProvider();

            using var scope = _provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NoaaCacheContext>();
            db.Database.EnsureCreated();

            return scope.ServiceProvider.GetRequiredService<NoaaService>();
        }

        private NoaaCacheContext GetDb()
        {
            var scope = _provider.CreateScope();
            return scope.ServiceProvider.GetRequiredService<NoaaCacheContext>();
        }

        public void Dispose()
        {
            _connection.Close();
        }

        // =========================
        // ACTIVE STORMS
        // =========================

        [Fact]
        public async Task GetActiveStormTracksAsync_ShouldReturnData()
        {
            _api.Setup(x => x.GetAsync<NhcActiveStormResponse>(
                It.IsAny<string>(),
                It.IsAny<ApiType>()))
                .ReturnsAsync(new NhcActiveStormResponse());

            var sut = CreateSut();

            var result = await sut.GetActiveStormTracksAsync();

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetActiveStormTracksAsync_ShouldThrowApiException()
        {
            _api.Setup(x => x.GetAsync<NhcActiveStormResponse>(
                It.IsAny<string>(),
                It.IsAny<ApiType>()))
                .ThrowsAsync(new Exception("fail"));

            var sut = CreateSut();

            await Assert.ThrowsAsync<ApiException>(() =>
                sut.GetActiveStormTracksAsync());
        }

        // =========================
        // CACHE HIT
        // =========================

        [Fact]
        public async Task GetHistoricalStormTracksAsync_ShouldReturnCachedData()
        {
            var sut = CreateSut();

            using (var db = GetDb())
            {
                db.StormTracks.Add(new StormTrack
                {
                    Id = "A",
                    Name = "StormA",
                    Season = 2020,
                    Track = new List<StormTrackPointItem>
                    {
                        new() { Time = DateTime.Now, Latitude = 10, Longitude = 10 }
                    }
                });

                await db.SaveChangesAsync();
            }

            var result = await sut.GetHistoricalStormTracksAsync(2020, CancellationToken.None);

            Assert.Single(result);

            _ibtracs.Verify(x => x.GetRemoteStreamAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        // =========================
        // CSV PARSE
        // =========================

        [Fact]
        public async Task GetHistoricalStormTracksAsync_ShouldParseAndSave()
        {
            // Added a second line (dummy units) to match the service's expectations
            var csv = @"SID,SEASON,LAT,LON,NAME,ISO_TIME,USA_WIND,USA_PRES,USA_SSHS,BASIN,NATURE
    (empty), (units), (units), (units), (units), (units), (units), (units), (units), (units), (units)
    A,2021,10,20,StormA,2021-01-01 00:00:00,50,1000,1,NA,TS";

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

            _ibtracs.Setup(x => x.GetRemoteStreamAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((stream, DateTime.UtcNow));

            _ibtracs.Setup(x => x.GetRemoteLastModifiedAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((DateTimeOffset?)null); // Match the DateTimeOffset type

            var sut = CreateSut();

            var result = await sut.GetHistoricalStormTracksAsync(2021, CancellationToken.None);

            Assert.Single(result);
            Assert.NotEmpty(result.First().Track);
            Assert.Equal("StormA", result.First().Name);
        }
        // =========================
        // CANCEL
        // =========================

        [Fact]
        public async Task GetHistoricalStormTracksAsync_ShouldRespectCancellation()
        {
            var sut = CreateSut();

            var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                sut.GetHistoricalStormTracksAsync(2021, cts.Token));
        }

        // =========================
        // PARSER EDGE CASE
        // =========================

        [Fact]
        public async Task Parser_ShouldSkipInvalidLatLon()
        {
            var csv = @"SID,SEASON,LAT,LON,NAME
A,2021,0,0,StormA";

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

            _ibtracs.Setup(x => x.GetRemoteStreamAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((stream, DateTime.UtcNow));

            _ibtracs.Setup(x => x.GetRemoteLastModifiedAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((DateTime?)null);

            var sut = CreateSut();

            var result = await sut.GetHistoricalStormTracksAsync(2021, CancellationToken.None);

            Assert.All(result, s => Assert.Empty(s.Track));
        }

        // =========================
        // CLEAR CACHE
        // =========================

        [Fact]
        public async Task ClearCacheAsync_ShouldDeleteAllData()
        {
            var sut = CreateSut();

            using (var db = GetDb())
            {
                db.StormTracks.Add(new StormTrack { Id = "A", Name = "StormA", Season = 2020 });
                db.Metadata.Add(new DbMetadata { Key = "test", LastValue = DateTime.UtcNow });

                await db.SaveChangesAsync();
            }

            await sut.ClearCacheAsync(CancellationToken.None);

            using (var db = GetDb())
            {
                Assert.Empty(db.StormTracks);
                Assert.Empty(db.Metadata);
            }
        }
    }
}