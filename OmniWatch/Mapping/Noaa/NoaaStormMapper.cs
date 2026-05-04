using OmniWatch.Integrations.Contracts.NOA;
using OmniWatch.Models.Noaa.ArchiveStorms;
using System.Collections.Generic;
using System.Linq;

namespace OmniWatch.Mapping.Noaa
{
    public static class NoaaStormMapper
    {
        public static StormTrackDto Map(StormTrack storm)
        {
            return new StormTrackDto
            {
                Id = storm.Id,
                Name = storm.Name,

                Track = storm.Track
                    .Select(MapPoint)
                    .ToList()
            };
        }

        public static List<StormTrackDto> Map(List<StormTrack> storms)
        {
            var result = new List<StormTrackDto>();

            if (storms == null || storms.Count == 0)
                return result;

            foreach (var storm in storms)
                result.Add(Map(storm));

            return result;
        }

        private static StormPointDto MapPoint(StormTrackPointItem point)
        {
            return new StormPointDto
            {
                Latitude = point.Latitude,
                Longitude = point.Longitude,
                Wind = point.Wind,
                Pressure = point.Pressure,
                Category = point.Category,
                Basin = point.Basin,
                DistanceToLand = point.DistanceToLand,
                Nature = point.Nature,
                Time = point.Time,
            };
        }
    }
}
