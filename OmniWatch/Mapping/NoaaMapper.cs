using OmniWatch.Integrations.Contracts.NOA;
using OmniWatch.Models.Noaa;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniWatch.Mapping
{
    public static class NoaaMapper
    {
        public static CycloneDto ToDto(this NoaaStormItem src)
        {
            return new CycloneDto
            {
                Latitude = src.LatitudeValue,
                Longitude = src.LongitudeValue,
                Forecast = (src.Forecast ?? new()).Select(f => f.ToDto()).ToList(),
                Cone = (src.Cone ?? new()).Select(c => c.ToDto()).ToList()
            };
        }

        public static CycloneForecastDto ToDto(this NoaaForecastItem src)
        {
            return new CycloneForecastDto
            {
                Time = src.TimeValue,
                Latitude = src.LatitudeValue,
                Longitude = src.LongitudeValue
            };
        }

        public static CyclonePointDto ToDto(this NoaaConePoint src)
        {
            return new CyclonePointDto
            {
                Latitude = src.LatitudeValue,
                Longitude = src.LongitudeValue
            };
        }

        public static CycloneConeDto ToDto(this NoaaConeItem src)
        {
            return new CycloneConeDto
            {
                Points = (src.Points ?? new()).Select(p => p.ToDto()).ToList()
            };
        }


    }
}
