using System;
using System.Collections.Generic;
using System.Text;

namespace OmniWatch.Models.Noaa.ActiveStorms
{
    public class ActiveStormDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Classification { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Intensity { get; set; }
        public string IntensityUnit { get; set; } = "KT";

        public double WindSpeedKM { get { return KnotsToKmH(Intensity); } }

        public int Pressure { get; set; }
        public string PressureUnit { get; set; } = "mb";

        public string Movement { get; set; }

        public string LastUpdate { get; set; }

        public List<StormProductDto> Links { get; set; } = new List<StormProductDto>();


        public static double KnotsToKmH(double knots)
        {
            return knots * 1.852;
        }
    }
}
