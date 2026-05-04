using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using OmniWatch.Core.Interfaces;
using OmniWatch.Integrations.Contracts.NOA;
using OmniWatch.Integrations.Enums;
using OmniWatch.Integrations.Interfaces;
using OmniWatch.Integrations.Persistence;
using OmniWatch.Integrations.Services;
using OmniWatch.Integrations.Tests.Handlers;
using System.Net;

namespace OmniWatch.Integrations.Tests.Services
{
    public class NoaaServiceTests : IDisposable
    {
        private readonly Mock<IHttpClientFactory> _factoryMock;
        private readonly Mock<IApiClient> _apiClientMock;
        private readonly Mock<IIbtracsClient> _ibtracsMock;
        private readonly Mock<IGlobalProgressService> _progressMock;
        private readonly NoaaCacheContext _db;
        private readonly SqliteConnection _connection;

        public NoaaServiceTests()
        {
            _factoryMock = new Mock<IHttpClientFactory>();
            _ibtracsMock = new Mock<IIbtracsClient>();
            _progressMock = new Mock<IGlobalProgressService>();
            _apiClientMock = new Mock<IApiClient>();

            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<NoaaCacheContext>()
                .UseSqlite(_connection)
                .Options;

            _db = new NoaaCacheContext(options);
            _db.Database.EnsureCreated();
        }

        private NoaaService CreateService(HttpClient client)
        {
            _factoryMock
                .Setup(f => f.CreateClient(ApiType.Noaa.ToString()))
                .Returns(client);

            return new NoaaService(
                _factoryMock.Object,
                _ibtracsMock.Object,
                _progressMock.Object,
                _apiClientMock.Object,
                _db);
        }

        // =========================
        // ACTIVE STORMS (KML)
        // =========================
        //[Fact]
        //public async Task GetActiveStormTracksAsync_Should_Parse_Kml_Correctly()
        //{
        //    // Arrange
        //    var kml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
        //        <kml xmlns=""http://www.opengis.net/kml/2.2"">
        //        <Document>
        //            <Placemark>
        //                <name>AL182024 - Hurricane Milton</name>
        //                <LineString>
        //                    <coordinates>-85.5,22.1,0 -84.2,23.5,0</coordinates>
        //                </LineString>
        //            </Placemark>
        //        </Document>
        //        </kml>";

        //    var handler = new FakeHttpMessageHandler(_ =>
        //        new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(kml) });

        //    var client = new HttpClient(handler) { BaseAddress = new Uri("https://noaa.test/") };
        //    var service = CreateService(client);

        //    // Act
        //    var result = await service.GetActiveStormTracksAsync();

        //    // Assert
        //    Assert.Single(result);
        //    Assert.Equal("AL182024 - Hurricane Milton", result[0].Name);
        //    Assert.Equal(2, result[0].Track.Count);
        //    Assert.Equal(22.1, result[0].Track[0].Latitude); // Lat é o segundo valor no KML (lon,lat)
        //}

        // =========================
        // CACHE LOGIC (DB)
        // =========================
        [Fact]
        public async Task GetHistoricalStormTracksAsync_Should_Return_Cached_Data_If_Exists()
        {
            // Arrange
            var year = 2024;
            var stormId = "AL182024"; // O ID da Storm continua string (ex: SID do IBTrACS)

            var cachedStorm = new StormTrack
            {
                Id = stormId,
                Name = "MILTON"
            };

            cachedStorm.Track.Add(new StormTrackPointItem
            {
                Time = new DateTime(year, 10, 5, 12, 0, 0, DateTimeKind.Utc),
                Latitude = 22.1,
                Longitude = -85.5,
                Basin = "NA",
                Nature = "TS"
            });

            _db.StormTracks.Add(cachedStorm);
            await _db.SaveChangesAsync();

            // Importante: Limpar o cache de rastreio para forçar leitura do SQLite
            _db.ChangeTracker.Clear();

            var service = CreateService(new HttpClient());

            // Act
            var result = await service.GetHistoricalStormTracksAsync(year, CancellationToken.None);

            // Assert
            Assert.NotEmpty(result);
            Assert.Equal(stormId, result[0].Id);

            // Se o banco retornou dados, o Mock do Client NUNCA deve ter sido chamado
            _ibtracsMock.Verify(x => x.GetLocalCsvPathAsync(), Times.Never);
        }

        [Fact]
        public async Task GetHistoricalStormTracksAsync_Should_Parse_Csv_And_Save_To_Db_On_Cache_Miss()
        {
            // Arrange
            var year = 2023;
            // O parser espera as colunas específicas que você definiu no FindColumn
            var csvContent =
                "SID,SEASON,NAME,LAT,LON,ISO_TIME,USA_WIND,USA_PRES,USA_SSHS,BASIN,NATURE\n" +
                "Header,Line,Units,,,,,,,,\n" +
                "2023001,2023,IDALIA,24.5,-83.1,2023-08-30 12:00:00,100,950,3,NA,TS";

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, csvContent);

            _ibtracsMock.Setup(x => x.GetLocalCsvPathAsync()).ReturnsAsync(tempFile);
            var service = CreateService(new HttpClient());

            // Act
            var result = await service.GetHistoricalStormTracksAsync(year, CancellationToken.None);

            // Assert
            Assert.Single(result);
            Assert.Equal("IDALIA", result[0].Name);

            // Verifica se salvou no DB real (SQLite em memória)
            var dbCount = await _db.StormTracks.CountAsync();
            Assert.Equal(1, dbCount);

            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }
    }
}