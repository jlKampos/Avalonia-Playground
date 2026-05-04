using System;
using System.Collections.Generic;
using System.Text;

namespace OmniWatch.Models.Noaa.ArchiveStorms
{
    public class StormTrackDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        public List<StormPointDto> Track { get; set; } = new();

        public int PointCount => Track.Count;
    }
}
