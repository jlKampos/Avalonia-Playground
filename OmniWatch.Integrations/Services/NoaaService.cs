using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using OmniWatch.Core.Interfaces;
using OmniWatch.Integrations.Contracts.Locations;
using OmniWatch.Integrations.Contracts.NOA;
using OmniWatch.Integrations.Contracts.NOA.ActiveStorms;
using OmniWatch.Integrations.Enums;
using OmniWatch.Integrations.Exceptions;
using OmniWatch.Integrations.Interfaces;
using OmniWatch.Integrations.Persistence;
using System.Globalization;
using System.Xml.Linq;

namespace OmniWatch.Integrations.Services
{
    public class NoaaService : INoaaService
    {
        private readonly HttpClient _client;
        private readonly IApiClient _apiClient;
        private readonly IIbtracsClient _ibtracsClient;
        private readonly IGlobalProgressService _progress;
        private readonly NoaaCacheContext _db;
        public NoaaService(
            IHttpClientFactory factory,
            IIbtracsClient ibtracsClient, IGlobalProgressService progress,
            IApiClient apiClient, NoaaCacheContext db)
        {
            _client = factory.CreateClient(ApiType.Noaa.ToString());
            _progress = progress;
            _ibtracsClient = ibtracsClient;
            _apiClient = apiClient;
            _db = db;
        }

        private void Report(string msg) => _progress.Report(msg);

        // ACTIVE STORMS (KML)
        public async Task<NhcActiveStormResponse> GetActiveStormTracksAsync()
        {

            try
            {
                return await _apiClient.GetAsync<NhcActiveStormResponse>("productexamples/NHC_JSON_Sample.json", ApiType.Noaa).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new ApiException("Failed to load active storms", ex);
            }
        }

        // HISTORICAL STORMS (IBTrACS)
        public async Task<List<StormTrack>> GetHistoricalStormTracksAsync(int year, CancellationToken cancellationToken, IProgress<string>? progress = null)
        {
            Report("Checking local data");

            var startOfYear = new DateTime(year, 1, 1);
            var endOfYear = new DateTime(year, 12, 31, 23, 59, 59);

            var cached = await _db.StormTracks
                 .AsNoTracking()
                 .Include(s => s.Track)
                 .Where(s => s.Track.Any(p => p.Time >= startOfYear && p.Time <= endOfYear))
                 .ToListAsync(cancellationToken);

            if (cached.Any()) return cached;

            var csvPath = await _ibtracsClient.GetLocalCsvPathAsync();

            var freshData = await Task.Run(() =>
            {
                using var reader = new StreamReader(csvPath);
                return ParseIbtracsStream(reader, year, cancellationToken);
            }, cancellationToken);

            if (freshData.Any() && !cancellationToken.IsCancellationRequested)
            {
                Report("Updating local database...");

                var strategy = _db.Database.CreateExecutionStrategy();

                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
                    try
                    {
                        var idsToRemove = freshData.Select(x => x.Id).ToList();

                        await _db.StormTracks
                            .Where(s => idsToRemove.Contains(s.Id))
                            .ExecuteDeleteAsync(cancellationToken);

                        await _db.StormTracks.AddRangeAsync(freshData, cancellationToken);
                        await _db.SaveChangesAsync(cancellationToken);

                        await transaction.CommitAsync(cancellationToken);
                    }
                    catch
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        throw;
                    }
                });
            }

            return freshData;
        }


        // =========================
        // IBTrACS PARSER (HISTORICAL)
        // =========================
        private List<StormTrack> ParseIbtracsStream(StreamReader reader, int year, CancellationToken cancellationToken)
        {
            Report("Parsing CSV data...");
            var stormDict = new Dictionary<string, StormTrack>();

            using var parser = new TextFieldParser(reader)
            {
                TextFieldType = FieldType.Delimited,
                HasFieldsEnclosedInQuotes = true
            };
            parser.SetDelimiters(",");

            // =========================================================
            // 1. FIND HEADER ROW
            // =========================================================
            string[]? headers = null;

            while (!parser.EndOfData)
            {
                // Check for cancellation before processing the next line
                if (cancellationToken.IsCancellationRequested) return [];

                var row = parser.ReadFields();
                if (row == null || row.Length < 5) continue;

                // Detect header by looking for the Latitude column
                if (row.Any(h => h.Equals("LAT", StringComparison.OrdinalIgnoreCase)))
                {
                    headers = row;
                    break;
                }
            }

            if (headers == null)
            {
                Report("Error: CSV headers not found.");
                return [];
            }

            // =========================================================
            // 2. COLUMN MAPPING
            // =========================================================
            int latIndex = FindColumn(headers, ["LAT", "LATITUDE"]);
            int lonIndex = FindColumn(headers, ["LON", "LONGITUDE"]);
            int idIndex = FindColumn(headers, ["SID"]);
            int nameIndex = FindColumn(headers, ["NAME"]);
            int seasonIndex = FindColumn(headers, ["SEASON", "YEAR"]);
            int timeIndex = FindColumn(headers, ["ISO_TIME", "TIME", "DATE"]);
            int windIndex = FindColumn(headers, ["USA_WIND", "WIND"]);
            int pressureIndex = FindColumn(headers, ["USA_PRES", "PRES"]);
            int categoryIndex = FindColumn(headers, ["USA_SSHS"]);
            int basinIndex = FindColumn(headers, ["BASIN"]);
            int natureIndex = FindColumn(headers, ["NATURE"]);

            // Required columns validation
            if (latIndex < 0 || lonIndex < 0 || seasonIndex < 0)
            {
                Report("Error: Missing required columns (Lat/Lon/Season).");
                return [];
            }

            // =========================================================
            // 3. INTERNAL SANITIZERS (Helpers)
            // =========================================================

            // Clean strings and handle null/empty/NA values
            static string Clean(string? v)
            {
                if (string.IsNullOrWhiteSpace(v)) return null;
                v = v.Trim();
                return (v == "-999" || v == "-99" || v == "NA") ? null : v;
            }

            static int SafeInt(string? v) =>
                int.TryParse(Clean(v), NumberStyles.Any, CultureInfo.InvariantCulture, out var i) ? i : 0;

            static double SafeDouble(string? v) =>
                double.TryParse(Clean(v), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0;

            static DateTime? SafeDate(string? v) =>
                DateTime.TryParse(Clean(v), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt) ? dt : null;

            // =========================================================
            // 4. DATA EXTRACTION LOOP
            // =========================================================
            while (!parser.EndOfData)
            {
                // CRITICAL: Stop processing immediately if user cancels/leaves the page
                if (cancellationToken.IsCancellationRequested)
                {
                    Report("Parsing operation aborted by user.");
                    return [];
                }

                var parts = parser.ReadFields();
                if (parts == null || parts.Length <= Math.Max(latIndex, lonIndex))
                    continue;

                // Filter by the requested year (Season)
                if (!int.TryParse(parts[seasonIndex], out var season) || season != year)
                    continue;

                var lat = SafeDouble(parts.ElementAtOrDefault(latIndex));
                var lon = SafeDouble(parts.ElementAtOrDefault(lonIndex));

                // Skip records with invalid coordinates
                if (lat == 0 && lon == 0) continue;

                var id = Clean(parts.ElementAtOrDefault(idIndex)) ?? "UNKNOWN";
                var name = Clean(parts.ElementAtOrDefault(nameIndex)) ?? "UNKNOWN";

                // Group points by Storm ID (SID)
                if (!stormDict.TryGetValue(id, out var storm))
                {
                    storm = new StormTrack { Id = id, Name = name };
                    stormDict[id] = storm;
                }

                // Parse and add point data
                storm.Track.Add(new StormTrackPointItem
                {
                    Time = (timeIndex >= 0 ? SafeDate(parts.ElementAtOrDefault(timeIndex)) : null) ?? DateTime.MinValue,
                    Latitude = lat,
                    Longitude = NormalizeLon(lon), // Normalizes longitude to [-180, 180]

                    Wind = windIndex >= 0 ? SafeInt(parts.ElementAtOrDefault(windIndex)) : 0,
                    Pressure = pressureIndex >= 0 ? SafeInt(parts.ElementAtOrDefault(pressureIndex)) : 0,
                    Category = categoryIndex >= 0 ? SafeInt(parts.ElementAtOrDefault(categoryIndex)) : 0,

                    Basin = (basinIndex >= 0 ? Clean(parts.ElementAtOrDefault(basinIndex)) : null) ?? "UNKNOWN",
                    Nature = (natureIndex >= 0 ? Clean(parts.ElementAtOrDefault(natureIndex)) : null) ?? "UNKNOWN"
                });
            }

            Report($"Parsing complete. Found {stormDict.Count} storms for year {year}.");
            return stormDict.Values.ToList();
        }


        //// =========================
        //// HELPERS
        //// =========================
        private static double NormalizeLon(double lon)
        {
            return lon > 180 ? lon - 360 : lon;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="headers"></param>
        /// <param name="possibleNames"></param>
        /// <returns></returns>
        private static int FindColumn(string[] headers, string[] possibleNames)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                foreach (var name in possibleNames)
                {
                    if (headers[i].Trim().Equals(name, StringComparison.OrdinalIgnoreCase))
                        return i;
                }
            }

            return -1;
        }
    }
}