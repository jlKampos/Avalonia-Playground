using OmniWatch.Integrations.Contracts.NOA;
using OmniWatch.Integrations.Interfaces;
using System.Globalization;

namespace OmniWatch.Integrations.Services
{
    public class IbtracsService : IIbtracsService
    {
        private readonly IIbtracsClient _client;

        public IbtracsService(IIbtracsClient client)
        {
            _client = client;
        }

        //public async Task<List<StormTrack>> LoadAsync()
        //{
        //    using var stream = await _client.DownloadCsvAsync();
        //    using var reader = new StreamReader(stream);

        //    var stormDict = new Dictionary<string, StormTrack>();

        //    string? line;
        //    bool headerSkipped = false;

        //    while ((line = await reader.ReadLineAsync()) != null)
        //    {
        //        if (!headerSkipped)
        //        {
        //            headerSkipped = true;
        //            continue;
        //        }

        //        var parts = line.Split(',');

        //        if (parts.Length < 10)
        //            continue;

        //        var id = parts[0];
        //        var name = parts[5];

        //        var lat = ParseDouble(parts[6]);
        //        var lon = ParseDouble(parts[7]);

        //        if (!stormDict.TryGetValue(id, out var storm))
        //        {
        //            storm = new StormTrack
        //            {
        //                Id = id,
        //                Name = name
        //            };

        //            stormDict[id] = storm;
        //        }

        //        storm.Track.Add(new StormTrackPointItem
        //        {
        //            Latitude = lat,
        //            Longitude = lon
        //        });
        //    }

        //    return stormDict.Values.ToList();
        //}

        private static double ParseDouble(string value)
        {
            return double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)
                ? v
                : 0;
        }
    }
}
