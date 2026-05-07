using System;

namespace OmniWatch.Models.Noaa.ArchiveStorms
{
    public class StormPointDto
    {
        public DateTime Time { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public int Wind { get; set; }

        public double WindSpeedKM { get { return KnotsToKmH(Wind); } }

        public int Pressure { get; set; }
        public int Category { get; set; }

        public string Basin { get; set; }
        public string Nature { get; set; }

        public double DistanceToLand { get; set; }

        private static double KnotsToKmH(double knots)
        {
            return knots * 1.852;
        }
    }
}
