using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

        // Keys for the Metadata table persistence
        private const string KEY_LAST_CHECK = "Last_Check_Timestamp";
        private const string KEY_IBTRACS_SYNC = "Ibtracs_Sync";

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
                //#if DEBUG
                //                endpoint = "productexamples/NHC_JSON_Sample.json";
                //#endif
                return await _apiClient.GetAsync<NhcActiveStormResponse>(endpoint, ApiType.Noaa);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, IL.Translation("Noaa_LoadActiveStormsFailed"));
                throw new ApiException(IL.Translation("Noaa_LoadActiveStormsError"), ex);
            }
        }

        public async Task<List<StormTrack>> GetHistoricalStormTracksAsync(int year, CancellationToken cancellationToken)
        {
            // 1. Check if an internet update check is required (Throttled to every 12h)
            await EnsureCacheIsFreshAsync(cancellationToken);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NoaaCacheContext>();

            // 2. Optimized local search using AsSplitQuery for better performance with large track datasets
            var cached = await db.StormTracks
                 .AsNoTracking()
                 .Include(s => s.Track.OrderBy(p => p.Time))
                 .AsSplitQuery()
                 .Where(s => s.Season == year)
                 .ToListAsync(cancellationToken);

            if (cached.Any()) return cached;

            // 3. If no local data exists for this year, download and parse
            Report(string.Format(IL.Translation("Noaa_YearNotFound"), year));
            var (stream, _) = await _ibtracsClient.GetRemoteStreamAsync(cancellationToken);

            using (stream)
            using (var reader = new StreamReader(stream))
            {
                await ParseAndCacheYearAsync(reader, year, cancellationToken);
            }

            // 4. Refresh data from DB after insertion to return the populated objects
            using var refreshScope = _scopeFactory.CreateScope();
            var dbRefresh = refreshScope.ServiceProvider.GetRequiredService<NoaaCacheContext>();

            return await dbRefresh.StormTracks
                 .AsNoTracking()
                 .Include(s => s.Track.OrderBy(p => p.Time))
                 .AsSplitQuery()
                 .Where(s => s.Season == year)
                 .ToListAsync(cancellationToken);
        }

        private async Task EnsureCacheIsFreshAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NoaaCacheContext>();

            // Check when the last internet verification was performed
            var lastCheckMeta = await db.Metadata.AsNoTracking().FirstOrDefaultAsync(m => m.Key == KEY_LAST_CHECK, ct);

            // Comparison using your DateTimeOffset model property
            if (lastCheckMeta != null && lastCheckMeta.LastValue > DateTimeOffset.UtcNow.AddHours(-12))
            {
                return; // Exit if checked recently to keep transitions instant
            }

            // Perform a lightweight HEAD request to check the last modified date on the NOAA server
            var remoteDate = await _ibtracsClient.GetRemoteLastModifiedAsync(ct);
            if (!remoteDate.HasValue) return;

            // Update the last check timestamp in the database
            await UpsertMetadataAsync(db, KEY_LAST_CHECK, DateTimeOffset.UtcNow, ct);

            // Compare remote date with the date of the last successful data synchronization
            var syncMeta = await db.Metadata.AsNoTracking().FirstOrDefaultAsync(m => m.Key == KEY_IBTRACS_SYNC, ct);

            if (syncMeta != null && remoteDate.Value <= syncMeta.LastValue)
            {
                return; // Local cache is already up to date with server
            }

            // Remote file is NEWER: Cache reset required to ensure data integrity
            Report(IL.Translation("Noaa_IbtracsUpdated"));

            var strategy = db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await db.Database.BeginTransactionAsync(ct);
                try
                {
                    _logger.LogWarning(IL.Translation("Noaa_IbtracsClearing"));

                    // Wipe old data to avoid inconsistencies with the updated global CSV
                    await db.StormPoints.ExecuteDeleteAsync(ct);
                    await db.StormTracks.ExecuteDeleteAsync(ct);

                    // Register the new synchronization date from the server
                    await UpsertMetadataAsync(db, KEY_IBTRACS_SYNC, remoteDate.Value, ct);

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

        private async Task UpsertMetadataAsync(NoaaCacheContext db, string key, DateTimeOffset value, CancellationToken ct)
        {
            var existing = await db.Metadata.FirstOrDefaultAsync(m => m.Key == key, ct);
            if (existing != null)
            {
                existing.LastValue = value;
            }
            else
            {
                db.Metadata.Add(new DbMetadata { Key = key, LastValue = value });
            }
            await db.SaveChangesAsync(ct);
        }

        private async Task ParseAndCacheYearAsync(StreamReader reader, int targetYear, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NoaaCacheContext>();

            // PERFORMANCE: Disable change tracking for high-volume bulk insertion
            db.ChangeTracker.AutoDetectChangesEnabled = false;

            // Cleanup for the target year to avoid duplicates
            var stormIdsForYear = db.StormTracks.Where(s => s.Season == targetYear).Select(s => s.Id);
            await db.StormPoints.Where(p => stormIdsForYear.Contains(p.StormTrackId)).ExecuteDeleteAsync(ct);
            await db.StormTracks.Where(s => s.Season == targetYear).ExecuteDeleteAsync(ct);

            string? headerLine = await reader.ReadLineAsync();
            string? unitLine = await reader.ReadLineAsync(); // Skip the units line in IBTrACS CSV

            if (headerLine == null) return;

            // Dynamic column mapping based on aliases
            var headers = headerLine.Split(',');
            int idIdx = FindColumn(headers, ["SID"]),
                nameIdx = FindColumn(headers, ["NAME"]),
                seasonIdx = FindColumn(headers, ["SEASON"]),
                latIdx = FindColumn(headers, ["LAT"]),
                lonIdx = FindColumn(headers, ["LON"]),
                timeIdx = FindColumn(headers, ["ISO_TIME"]),
                windIdx = FindColumn(headers, ["USA_WIND", "WIND"]),
                presIdx = FindColumn(headers, ["USA_PRES", "PRES"]),
                catIdx = FindColumn(headers, ["USA_SSHS"]),
                basinIdx = FindColumn(headers, ["BASIN"]),
                natureIdx = FindColumn(headers, ["NATURE"]),
                distIdx = FindColumn(headers, ["DIST2LAND"]);

            using var transaction = await db.Database.BeginTransactionAsync(ct);

            var stormIdsInContext = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string targetYearStr = targetYear.ToString();
            int count = 0;
            string? line;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (ct.IsCancellationRequested) break;
                if (string.IsNullOrWhiteSpace(line)) continue;

                var row = line.Split(',');
                // Filter rows strictly by the requested year
                if (row.Length <= seasonIdx || row[seasonIdx] != targetYearStr) continue;

                string sid = row[idIdx].Trim();

                // Manage StormTrack (Parent entity)
                if (!stormIdsInContext.Contains(sid))
                {
                    db.StormTracks.Add(new StormTrack
                    {
                        Id = sid,
                        Name = GetSafe(row, nameIdx),
                        Season = targetYear
                    });
                    stormIdsInContext.Add(sid);
                }

                // Parse and add the StormPoint (Child entity)
                if (double.TryParse(row[latIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out var lat) &&
                    double.TryParse(row[lonIdx], NumberStyles.Any, CultureInfo.InvariantCulture, out var lon))
                {
                    db.StormPoints.Add(new StormTrackPointItem
                    {
                        StormTrackId = sid,
                        Time = DateTime.TryParse(row[timeIdx], CultureInfo.InvariantCulture, out var t) ? t : DateTime.MinValue,
                        Latitude = lat,
                        Longitude = lon,
                        Wind = SafeInt(GetSafe(row, windIdx)),
                        Pressure = SafeInt(GetSafe(row, presIdx)),
                        Category = SafeInt(GetSafe(row, catIdx)),
                        Basin = GetSafe(row, basinIdx) ?? "NA",
                        Nature = GetSafe(row, natureIdx) ?? "NA",
                        DistanceToLand = SafeDouble(GetSafe(row, distIdx))
                    });
                }

                // Batch Save every 1000 records to maintain memory stability during massive CSV parsing
                if (++count % 1000 == 0)
                {
                    await db.SaveChangesAsync(ct);
                    db.ChangeTracker.Clear();
                }
            }

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation("Import completed: {Count} points registered for year {Year}", count, targetYear);
        }

        public async Task ClearCacheAsync(CancellationToken cancellationToken)
        {
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
        private static double SafeDouble(string value) => double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0;
        private static string GetSafe(string[] row, int index) => index >= 0 && index < row.Length ? row[index] : string.Empty;

        private int FindColumn(string[] headers, string[] aliases)
        {
            for (int i = 0; i < headers.Length; i++)
                if (aliases.Any(a => a.Equals(headers[i].Trim(), StringComparison.OrdinalIgnoreCase))) return i;
            return -1;
        }
    }
}