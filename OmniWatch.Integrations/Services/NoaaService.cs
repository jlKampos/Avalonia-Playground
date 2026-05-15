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
using OmniWatch.Integrations.Localization;
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
                var endpoint = "CurrentStorms.json";

#if DEBUG
                endpoint = "productexamples/NHC_JSON_Sample.json";
#endif

                return await _apiClient.GetAsync<NhcActiveStormResponse>(
                    endpoint, ApiType.Noaa);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, IL.Translation("Noaa_LoadActiveStormsFailed"));
                throw new ApiException(IL.Translation("Noaa_LoadActiveStormsError"), ex);
            }
        }

        public async Task<List<StormTrack>> GetHistoricalStormTracksAsync(int year, CancellationToken cancellationToken)
        {
            await EnsureCacheIsFreshAsync(cancellationToken);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NoaaCacheContext>();

            Report(string.Format(IL.Translation("Noaa_SearchingYear"), year));

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

            Report(string.Format(IL.Translation("Noaa_YearNotFound"), year));

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

            Report(IL.Translation("Noaa_IbtracsUpdated"));

            var strategy = db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await db.Database.BeginTransactionAsync(ct);
                try
                {
                    _logger.LogWarning(IL.Translation("Noaa_IbtracsClearing"));
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
                    _logger.LogError(ex, IL.Translation("Noaa_TransactionFailed"));
                    await transaction.RollbackAsync(ct);
                    throw;
                }
            });
        }

        private async Task ParseAndCacheYearAsync(StreamReader reader, int targetYear, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NoaaCacheContext>();

            // 1. Disable tracking for massive speed boost during bulk inserts
            db.ChangeTracker.AutoDetectChangesEnabled = false;

            // 2. Load EVERY ID currently in the database for this year into the HashSet
            // This is the most reliable way to avoid UNIQUE constraint violations
            var stormIdsInDb = new HashSet<string>(
                await db.StormTracks
                    .AsNoTracking()
                    .Where(s => s.Season == targetYear)
                    .Select(s => s.Id)
                    .ToListAsync(ct),
                StringComparer.OrdinalIgnoreCase // Ensure case-insensitive matching
            );

            using var parser = new TextFieldParser(reader) { TextFieldType = FieldType.Delimited };
            parser.SetDelimiters(",");

            // Skip to headers
            string[]? headers = null;
            while (!parser.EndOfData)
            {
                var row = parser.ReadFields();
                if (row?.Any(h => h.Equals("LAT", StringComparison.OrdinalIgnoreCase)) == true)
                {
                    headers = row;
                    break;
                }
            }

            if (headers == null) return;

            // Indices (FindColumn is your helper method)
            int idIdx = FindColumn(headers, ["SID"]), nameIdx = FindColumn(headers, ["NAME"]),
                seasonIdx = FindColumn(headers, ["SEASON"]), latIdx = FindColumn(headers, ["LAT"]),
                lonIdx = FindColumn(headers, ["LON"]), timeIdx = FindColumn(headers, ["ISO_TIME"]),
                basinIdx = FindColumn(headers, ["BASIN"]), windIdx = FindColumn(headers, ["USA_WIND", "WIND"]),
                presIdx = FindColumn(headers, ["USA_PRES", "PRES"]), catIdx = FindColumn(headers, ["USA_SSHS"]),
                natureIdx = FindColumn(headers, ["NATURE"]), distIdx = FindColumn(headers, ["DIST2LAND"]);

            int count = 0;
            string targetYearStr = targetYear.ToString();

            while (!parser.EndOfData)
            {
                if (ct.IsCancellationRequested) break;

                var row = parser.ReadFields();
                if (row == null || row.Length <= seasonIdx) continue;

                // SKIP rows from other years
                if (row[seasonIdx] != targetYearStr) continue;

                string sid = row[idIdx];

                // 3. Double-check before adding
                if (!stormIdsInDb.Contains(sid))
                {
                    db.StormTracks.Add(new StormTrack
                    {
                        Id = sid,
                        Name = row[nameIdx],
                        Season = targetYear
                    });

                    // Add to HashSet immediately so we don't try to add it again 
                    // if it appears on the next line of the CSV
                    stormIdsInDb.Add(sid);
                }

                // Coordinate parsing...
                if (!double.TryParse(row[latIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out var lat) ||
                    !double.TryParse(row[lonIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out var lon))
                    continue;

                if (lat == 0 && lon == 0) continue;

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

                // 4. Frequent saves to keep the transaction small
                if (++count % 1000 == 0)
                {
                    Report(string.Format(IL.Translation("Noaa_SavingRecords"), count));
                    await db.SaveChangesAsync(ct);
                    // Clear to prevent the ChangeTracker from growing and slowing down
                    db.ChangeTracker.Clear();
                }
            }

            await db.SaveChangesAsync(ct);
            Report(IL.Translation("Noaa_YearSyncCompleted"));
        }

        public async Task ClearCacheAsync(CancellationToken cancellationToken)
        {
            _logger.LogWarning(IL.Translation("Noaa_ManualClearRequested"));

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NoaaCacheContext>();

            var strategy = db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    Report(IL.Translation("Noaa_CleaningDatabase"));

                    await db.StormPoints.ExecuteDeleteAsync(cancellationToken);
                    await db.StormTracks.ExecuteDeleteAsync(cancellationToken);
                    await db.Metadata.ExecuteDeleteAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation(IL.Translation("Noaa_CacheCleared"));
                    Report(IL.Translation("Noaa_CacheClearedDownload"));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, IL.Translation("Noaa_ClearCacheFailed"));
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
