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
            if (storm == null) return null;
            var uniquePoints = new List<StormPointDto>();
            var sortedSourcePoints = storm.Track.OrderBy(p => p.Time).ToList();

            foreach (var point in sortedSourcePoints)
            {
                var mappedPoint = MapPoint(point);

                if (uniquePoints.Count > 0)
                {
                    var lastPoint = uniquePoints.Last();
                    if (lastPoint.Latitude == mappedPoint.Latitude &&
                        lastPoint.Longitude == mappedPoint.Longitude)
                    {
                        continue;
                    }
                }

                uniquePoints.Add(mappedPoint);
            }

            return new StormTrackDto
            {
                Id = storm.Id,
                Name = storm.Name,
                Track = uniquePoints
            };
        }

        public static List<StormTrackDto> Map(List<StormTrack> storms)
        {
            var result = new List<StormTrackDto>();

            if (storms == null || storms.Count == 0)
                return result;

            foreach (var storm in storms)
            {
                var mapped = Map(storm);
                if (mapped != null && mapped.Track.Any())
                    result.Add(mapped);
            }

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
