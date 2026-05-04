using Microsoft.EntityFrameworkCore;
using Moq;
using OmniWatch.Core.Interfaces;
using OmniWatch.Integrations.Contracts.NOA;
using OmniWatch.Integrations.Contracts.NOA.ActiveStorms;
using OmniWatch.Integrations.Exceptions;
using OmniWatch.Integrations.Interfaces;
using OmniWatch.Integrations.Persistence;
using OmniWatch.Integrations.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OmniWatch.Integrations.Tests.Services;

public class NoaaServiceTests
{
    private readonly Mock<IHttpClientFactory> _factory = new();
    private readonly Mock<IIbtracsClient> _ibtracs = new();
    private readonly Mock<IGlobalProgressService> _progress = new();
    private readonly Mock<IApiClient> _api = new();

    private NoaaCacheContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<NoaaCacheContext>()
            .UseSqlite("Filename=:memory:")
            .Options;

        var db = new NoaaCacheContext(options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();

        return db;
    }

    private NoaaService CreateSut(NoaaCacheContext db)
    {
        _factory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient());

        return new NoaaService(
            _factory.Object,
            _ibtracs.Object,
            _progress.Object,
            _api.Object,
            db
        );
    }

    // =========================
    // ACTIVE STORMS
    // =========================

    [Fact]
    public async Task GetActiveStormTracksAsync_Should_Return_Data()
    {
        _api.Setup(x => x.GetAsync<NhcActiveStormResponse>(
                It.IsAny<string>(),
                It.IsAny<Integrations.Enums.ApiType>()))
            .ReturnsAsync(new NhcActiveStormResponse());

        var sut = CreateSut(CreateDb());

        var result = await sut.GetActiveStormTracksAsync();

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetActiveStormTracksAsync_Should_Throw_ApiException_On_Error()
    {
        _api.Setup(x => x.GetAsync<NhcActiveStormResponse>(
                It.IsAny<string>(),
                It.IsAny<Integrations.Enums.ApiType>()))
            .ThrowsAsync(new Exception("fail"));

        var sut = CreateSut(CreateDb());

        await Assert.ThrowsAsync<ApiException>(() =>
            sut.GetActiveStormTracksAsync());
    }

    // =========================
    // HISTORICAL - CACHE HIT
    // =========================

    [Fact]
    public async Task GetHistoricalStormTracksAsync_Should_Return_Cached_Data()
    {
        var db = CreateDb();

        db.StormTracks.Add(new StormTrack
        {
            Id = "A",
            Name = "StormA",
            Track = new List<StormTrackPointItem>
            {
                new StormTrackPointItem
                {
                    Time = new DateTime(2020,1,1),
                    Latitude = 10,
                    Longitude = 10,
                    Basin = "NA",
                    Nature = "TS"
                }
            }
        });

        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var result = await sut.GetHistoricalStormTracksAsync(
            2020,
            CancellationToken.None);

        Assert.Single(result);

        _ibtracs.Verify(x => x.GetLocalCsvPathAsync(), Times.Never);
    }

    // =========================
    // HISTORICAL - CSV LOAD
    // =========================

    [Fact]
    public async Task GetHistoricalStormTracksAsync_Should_Parse_Csv_And_Save()
    {
        var db = CreateDb();

        var csv = BuildValidCsv();

        var path = Path.GetTempFileName();
        await File.WriteAllTextAsync(path, csv);

        _ibtracs.Setup(x => x.GetLocalCsvPathAsync())
            .ReturnsAsync(path);

        var sut = CreateSut(db);

        var result = await sut.GetHistoricalStormTracksAsync(
            2021,
            CancellationToken.None);

        Assert.Single(result);

        var saved = await db.StormTracks
            .Include(x => x.Track)
            .ToListAsync();

        Assert.Single(saved);
        Assert.True(saved.First().Track.Count > 0);
    }

    // =========================
    // HISTORICAL - CANCEL
    // =========================

    [Fact]
    public async Task GetHistoricalStormTracksAsync_Should_Respect_Cancellation()
    {
        var db = CreateDb();

        var sut = CreateSut(db);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            sut.GetHistoricalStormTracksAsync(2021, cts.Token));
    }

    // =========================
    // PARSER EDGE CASES
    // =========================

    [Fact]
    public async Task Parser_Should_Skip_Invalid_LatLon()
    {
        var db = CreateDb();

        var csv = @"SID,SEASON,LAT,LON,NAME
A,2021,0,0,StormA";

        var path = Path.GetTempFileName();
        await File.WriteAllTextAsync(path, csv);

        _ibtracs.Setup(x => x.GetLocalCsvPathAsync())
            .ReturnsAsync(path);

        var sut = CreateSut(db);

        var result = await sut.GetHistoricalStormTracksAsync(
            2021,
            CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Parser_Should_Handle_Missing_Headers()
    {
        var db = CreateDb();

        var csv = @"INVALID,DATA
1,2";

        var path = Path.GetTempFileName();
        await File.WriteAllTextAsync(path, csv);

        _ibtracs.Setup(x => x.GetLocalCsvPathAsync())
            .ReturnsAsync(path);

        var sut = CreateSut(db);

        var result = await sut.GetHistoricalStormTracksAsync(
            2021,
            CancellationToken.None);

        Assert.Empty(result);
    }

    // =========================
    // HELPERS
    // =========================

    private static string BuildValidCsv()
    {
        return @"SID,SEASON,LAT,LON,NAME,ISO_TIME,USA_WIND,USA_PRES,USA_SSHS,BASIN,NATURE
            A,2021,10,20,StormA,2021-01-01 00:00:00,50,1000,1,NA,TS
            A,2021,11,21,StormA,2021-01-01 06:00:00,60,990,2,NA,TS";
    }
}