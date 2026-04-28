using Microsoft.VisualBasic.FileIO;
using OmniWatch.Core.Interfaces;
using OmniWatch.Integrations.Contracts.NOA;
using OmniWatch.Integrations.Enums;
using OmniWatch.Integrations.Interfaces;
using System.Globalization;
using System.Xml.Linq;

namespace OmniWatch.Integrations.Services
{
    public class NoaaService : INoaaService
    {
        private readonly HttpClient _client;
        private readonly IIbtracsClient _ibtracsClient;
        private readonly IGlobalProgressService _progress;
        public NoaaService(
            IHttpClientFactory factory,
            IIbtracsClient ibtracsClient, IGlobalProgressService progress)
        {
            _client = factory.CreateClient(ApiType.Noaa.ToString());
            _progress = progress;
            _ibtracsClient = ibtracsClient;
        }

        private void Report(string msg) => _progress.Report(msg);

        // ACTIVE STORMS (KML)
        public async Task<List<StormTrack>> GetActiveStormTracksAsync()
        {
            var kml = await _client.GetStringAsync("gis/kml/nhc_active.kml");

            return ParseKml(kml);
        }

        // HISTORICAL STORMS (IBTrACS)
        public async Task<List<StormTrack>> GetHistoricalStormTracksAsync(int year, IProgress<string>? progress = null)
        {
            Report("Downloading CSV…");
            var csvPath = await _ibtracsClient.GetLocalCsvPathAsync();
            using var reader = new StreamReader(csvPath);

            return await Task.Run(() =>
            {
                return ParseIbtracsStream(reader, year);
            });
        }

        // =========================
        // KML PARSER (ACTIVE)
        // =========================
        private List<StormTrack> ParseKml(string kml)
        {
            XDocument doc = XDocument.Parse(kml);

            XNamespace ns = "http://www.opengis.net/kml/2.2";

            var tracks = new List<StormTrack>();

            var placemarks = doc.Descendants(ns + "Placemark");

            foreach (var pm in placemarks)
            {
                var name = pm.Element(ns + "name")?.Value ?? "UNKNOWN";

                var lineString = pm.Descendants(ns + "LineString").FirstOrDefault();

                if (lineString == null)
                    continue;

                var coordsText = lineString.Element(ns + "coordinates")?.Value;

                if (string.IsNullOrWhiteSpace(coordsText))
                    continue;

                tracks.Add(new StormTrack
                {
                    Name = name,
                    Track = ParseCoordinates(coordsText)
                });
            }

            return tracks;
        }

        // =========================
        // COORDINATES PARSER
        // =========================
        private List<StormTrackPointItem> ParseCoordinates(string coords)
        {
            var result = new List<StormTrackPointItem>();

            var lines = coords.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var parts = line.Split(',');

                if (parts.Length < 2)
                    continue;

                if (double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var lat) &&
                    double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var lon))
                {
                    result.Add(new StormTrackPointItem
                    {
                        Latitude = lat,
                        Longitude = lon
                    });
                }
            }

            return result;
        }

        // =========================
        // IBTrACS PARSER (HISTORICAL)
        // =========================
        private List<StormTrack> ParseIbtracsStream(StreamReader reader, int year)
        {
            Report("Parsing CSV…");
            var stormDict = new Dictionary<string, StormTrack>();

            using var parser = new TextFieldParser(reader)
            {
                TextFieldType = FieldType.Delimited,
                HasFieldsEnclosedInQuotes = true
            };

            parser.SetDelimiters(",");

            // =========================
            // 1. FIND HEADER ROW
            // =========================
            string[]? headers = null;

            while (!parser.EndOfData)
            {
                var row = parser.ReadFields();

                if (row == null || row.Length < 5)
                    continue;

                if (row.Any(h =>
                    h.Equals("LAT", StringComparison.OrdinalIgnoreCase) ||
                    h.Equals("LON", StringComparison.OrdinalIgnoreCase)))
                {
                    headers = row;
                    break;
                }
            }

            if (headers == null)
                return new List<StormTrack>();

            // =========================
            // 2. MAP COLUMNS
            // =========================
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

            if (latIndex < 0 || lonIndex < 0 || seasonIndex < 0)
                return new List<StormTrack>();

            // =========================
            // 3. SANITIZER
            // =========================
            static string Clean(string? v)
            {
                if (string.IsNullOrWhiteSpace(v)) return null;
                if (v == "-999" || v == "-99" || v == "NA") return null;
                return v.Trim();
            }

            static int SafeInt(string? v)
            {
                v = Clean(v);
                return int.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var i) ? i : 0;
            }

            static double SafeDouble(string? v)
            {
                v = Clean(v);
                return double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0;
            }

            static DateTime? SafeDate(string? v)
            {
                v = Clean(v);
                if (v == null) return null;

                return DateTime.TryParse(v, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal, out var dt)
                    ? dt
                    : null;
            }

            // =========================
            // 4. PARSE DATA
            // =========================
            while (!parser.EndOfData)
            {
                var parts = parser.ReadFields();
                if (parts == null)
                    continue;

                if (parts.Length <= Math.Max(latIndex, lonIndex))
                    continue;

                if (!int.TryParse(parts[seasonIndex], out var season) || season != year)
                    continue;

                var lat = SafeDouble(parts.ElementAtOrDefault(latIndex));
                var lon = SafeDouble(parts.ElementAtOrDefault(lonIndex));

                if (lat == 0 && lon == 0)
                    continue;

                var id = Clean(parts.ElementAtOrDefault(idIndex)) ?? "UNKNOWN";
                var name = Clean(parts.ElementAtOrDefault(nameIndex)) ?? "UNKNOWN";

                if (!stormDict.TryGetValue(id, out var storm))
                {
                    storm = new StormTrack
                    {
                        Id = id,
                        Name = name
                    };

                    stormDict[id] = storm;
                }

                var time = timeIndex >= 0 && timeIndex < parts.Length
                    ? SafeDate(parts[timeIndex])
                    : null;

                var basin = basinIndex >= 0 ? Clean(parts.ElementAtOrDefault(basinIndex)) : null;
                var nature = natureIndex >= 0 ? Clean(parts.ElementAtOrDefault(natureIndex)) : null;

                storm.Track.Add(new StormTrackPointItem
                {
                    Time = time ?? DateTime.MinValue,

                    Latitude = lat,
                    Longitude = NormalizeLon(lon),

                    Wind = windIndex >= 0 ? SafeInt(parts.ElementAtOrDefault(windIndex)) : 0,
                    Pressure = pressureIndex >= 0 ? SafeInt(parts.ElementAtOrDefault(pressureIndex)) : 0,
                    Category = categoryIndex >= 0 ? SafeInt(parts.ElementAtOrDefault(categoryIndex)) : 0,

                    Basin = basin ?? "UNKNOWN",
                    Nature = nature ?? "UNKNOWN",

                    DistanceToLand = 0 // opcional futuro cálculo
                });
            }

            Report("Parsing complete.");
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