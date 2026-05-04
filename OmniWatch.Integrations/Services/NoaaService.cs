using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using OmniWatch.Core.Interfaces;
using OmniWatch.Integrations.Contracts.NOA;
using OmniWatch.Integrations.Contracts.NOA.ActiveStorms;
using OmniWatch.Integrations.Enums;
using OmniWatch.Integrations.Exceptions;
using OmniWatch.Integrations.Interfaces;
using OmniWatch.Integrations.Persistence;
using System.Globalization;

namespace OmniWatch.Integrations.Services
{
    public class NoaaService : INoaaService
    {
        private readonly IApiClient _apiClient;
        private readonly IIbtracsClient _ibtracsClient;
        private readonly IGlobalProgressService _globalProgress;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NoaaService> _logger;

        public NoaaService(
            IIbtracsClient ibtracsClient,
            IGlobalProgressService globalProgress,
            IApiClient apiClient,
            IServiceScopeFactory scopeFactory,
            ILogger<NoaaService> logger)
        {
            _globalProgress = globalProgress;
            _ibtracsClient = ibtracsClient;
            _apiClient = apiClient;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        private void Report(string msg)
        {
            _logger.LogInformation("Progress: {Message}", msg);
            _globalProgress.Report(msg);
        }

        public async Task<NhcActiveStormResponse> GetActiveStormTracksAsync()
        {
            try
            {
                return await _apiClient.GetAsync<NhcActiveStormResponse>(
                    "productexamples/NHC_JSON_Sample.json", ApiType.Noaa);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load active storms from NOAA.");
                throw new ApiException("Error loading active NOAA storms.", ex);
            }
        }

        public async Task<List<StormTrack>> GetHistoricalStormTracksAsync(int year, CancellationToken cancellationToken)
        {
            await EnsureCacheIsFreshAsync(cancellationToken);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NoaaCacheContext>();

            Report($"Searching for {year} data in local database...");
            var cached = await db.StormTracks
                 .AsNoTracking()
                 .Include(s => s.Track.OrderBy(p => p.Time))
                 .Where(s => s.Season == year)
                 .ToListAsync(cancellationToken);

            if (cached.Any())
            {
                _logger.LogInformation("Found {Count} storms in cache for year {Year}", cached.Count, year);
                return cached;
            }

            Report($"Data for {year} not found. Processing from NOAA remote server...");
            var (stream, lastModified) = await _ibtracsClient.GetRemoteStreamAsync(cancellationToken);

            using (stream)
            using (var reader = new StreamReader(stream))
            {
                await Task.Run(async () => await ParseAndCacheYearAsync(reader, year, cancellationToken), cancellationToken);
            }

            using var scope2 = _scopeFactory.CreateScope();
            var db2 = scope2.ServiceProvider.GetRequiredService<NoaaCacheContext>();

            return await db2.StormTracks
                 .AsNoTracking()
                 .Include(s => s.Track.OrderBy(p => p.Time))
                 .Where(s => s.Season == year)
                 .ToListAsync(cancellationToken);
        }

        private async Task EnsureCacheIsFreshAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NoaaCacheContext>();

            var remoteDate = await _ibtracsClient.GetRemoteLastModifiedAsync(ct);
            if (!remoteDate.HasValue) return;

            var localMeta = await db.Metadata.AsNoTracking().FirstOrDefaultAsync(m => m.Key == "Ibtracs_Sync", ct);

            if (localMeta != null && remoteDate <= localMeta.LastValue) return;

            Report("Dataset IBTrACS updated on server. Refreshing global cache...");

            var strategy = db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await db.Database.BeginTransactionAsync(ct);
                try
                {
                    _logger.LogWarning("IBTrACS out of date. Clearing all historical tables.");
                    await db.StormTracks.ExecuteDeleteAsync(ct);

                    var isoDate = remoteDate.Value.ToString("o");
                    await db.Database.ExecuteSqlRawAsync(
                        "INSERT INTO Metadata (Key, LastValue) VALUES ('Ibtracs_Sync', {0}) " +
                        "ON CONFLICT(Key) DO UPDATE SET LastValue = {0};",
                        isoDate);

                    await transaction.CommitAsync(ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Transaction failed while refreshing cache.");
                    await transaction.RollbackAsync(ct);
                    throw;
                }
            });
        }

        private async Task ParseAndCacheYearAsync(StreamReader reader, int targetYear, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NoaaCacheContext>();
            db.ChangeTracker.AutoDetectChangesEnabled = false;

            var stormIdsInDb = new HashSet<string>();
            using var parser = new TextFieldParser(reader) { TextFieldType = FieldType.Delimited };
            parser.SetDelimiters(",");

            string[]? headers = null;
            while (!parser.EndOfData)
            {
                var row = parser.ReadFields();
                if (row?.Any(h => h.Equals("LAT", StringComparison.OrdinalIgnoreCase)) == true) { headers = row; break; }
            }
            if (headers == null) return;

            int idIdx = FindColumn(headers, ["SID"]), nameIdx = FindColumn(headers, ["NAME"]), seasonIdx = FindColumn(headers, ["SEASON"]),
                latIdx = FindColumn(headers, ["LAT"]), lonIdx = FindColumn(headers, ["LON"]), timeIdx = FindColumn(headers, ["ISO_TIME"]),
                basinIdx = FindColumn(headers, ["BASIN"]), windIdx = FindColumn(headers, ["USA_WIND", "WIND"]),
                presIdx = FindColumn(headers, ["USA_PRES", "PRES"]), catIdx = FindColumn(headers, ["USA_SSHS"]),
                natureIdx = FindColumn(headers, ["NATURE"]), distIdx = FindColumn(headers, ["DIST2LAND"]);

            int count = 0;
            while (!parser.EndOfData)
            {
                if (ct.IsCancellationRequested) break;
                var row = parser.ReadFields();
                if (row == null || !int.TryParse(row[seasonIdx], out int rowSeason) || rowSeason != targetYear) continue;

                string sid = row[idIdx];
                if (!stormIdsInDb.Contains(sid))
                {
                    if (!await db.StormTracks.AnyAsync(x => x.Id == sid, ct))
                        db.StormTracks.Add(new StormTrack { Id = sid, Name = row[nameIdx], Season = targetYear });
                    stormIdsInDb.Add(sid);
                }

                if (!double.TryParse(row[latIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out var lat) ||
                    !double.TryParse(row[lonIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out var lon)) continue;

                if (lat == 0 && lon == 0)
                {
                    _logger.LogWarning("Invalid 0,0 coordinate skipped for storm {SID}", sid);
                    continue;
                }

                db.StormPoints.Add(new StormTrackPointItem
                {
                    StormTrackId = sid,
                    Time = DateTime.Parse(row[timeIdx], CultureInfo.InvariantCulture),
                    Latitude = lat,
                    Longitude = lon,
                    Wind = SafeInt(GetSafe(row, windIdx)),
                    Pressure = SafeInt(GetSafe(row, presIdx)),
                    Category = SafeInt(GetSafe(row, catIdx)),
                    Basin = GetSafe(row, basinIdx) ?? "NA",
                    Nature = GetSafe(row, natureIdx) ?? "NA",
                    DistanceToLand = SafeDouble(GetSafe(row, distIdx))
                });

                if (++count % 5000 == 0)
                {
                    Report($"Saving records... ({count} points processed)");
                    await db.SaveChangesAsync(ct);
                    db.ChangeTracker.Clear();
                }
            }

            await db.SaveChangesAsync(ct);
            Report("Year synchronization completed.");
        }



        public async Task ClearCacheAsync(CancellationToken cancellationToken)
        {
            _logger.LogWarning("Manual cache clear requested by user.");

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NoaaCacheContext>();

            var strategy = db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    Report("Cleaning the database...");

                    await db.StormPoints.ExecuteDeleteAsync(cancellationToken);

                    await db.StormTracks.ExecuteDeleteAsync(cancellationToken);

                    await db.Metadata.ExecuteDeleteAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation("Cache cleared successfully.");
                    Report("Cache cleared successfully. The app will download new data.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to clear cache.");
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }


        private static int SafeInt(string value) => int.TryParse(value, out var n) ? n : 0;

        private static string GetSafe(string[] row, int index)
        {
            return index >= 0 && index < row.Length ? row[index] : string.Empty;
        }
        private static double SafeDouble(string value) => double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0;

        private int FindColumn(string[] headers, string[] aliases)
        {
            for (int i = 0; i < headers.Length; i++)
                if (aliases.Any(a => a.Equals(headers[i].Trim(), StringComparison.OrdinalIgnoreCase))) return i;
            return -1;
        }
    }
}
