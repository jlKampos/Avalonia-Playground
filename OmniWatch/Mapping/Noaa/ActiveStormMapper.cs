using OmniWatch.Integrations.Contracts.NOA.ActiveStorms;
using OmniWatch.Models.Noaa.ActiveStorms;
using System.Collections.Generic;
using System.Linq;

namespace OmniWatch.Mapping.Noaa
{
    public static class ActiveStormMapper
    {
        public static ActiveStormDto Map(ActiveStormItem item)
        {
            if (item == null) return null;

            // Conversão segura das strings do JSON para os ints do DTO
            int.TryParse(item.Intensity, out var intensityValue);
            int.TryParse(item.Pressure, out var pressureValue);

            var dto = new ActiveStormDto
            {
                Id = item.Id,
                Name = item.Name,
                Classification = item.Classification,
                Latitude = item.LatitudeNumeric,
                Longitude = item.LongitudeNumeric,
                Intensity = intensityValue,
                Pressure = pressureValue,
                LastUpdate = item.LastUpdate,
                // Formatação do movimento usando as propriedades existentes
                Movement = $"{item.MovementDir}° @ {item.MovementSpeed} kt",
                Links = new List<StormProductDto>()
            };

            // Mapeia apenas o que existe no teu ActiveStormItem
            if (item.PublicAdvisory != null && !string.IsNullOrEmpty(item.PublicAdvisory.Url))
            {
                dto.Links.Add(new StormProductDto
                {
                    Label = "Public Advisory",
                    Url = item.PublicAdvisory.Url,
                    Type = "Text"
                });
            }

            if (item.ForecastGraphics != null && !string.IsNullOrEmpty(item.ForecastGraphics.Url))
            {
                dto.Links.Add(new StormProductDto
                {
                    Label = "Forecast Graphics",
                    Url = item.ForecastGraphics.Url,
                    Type = "Image"
                });
            }

            return dto;
        }

        public static List<ActiveStormDto> Map(List<ActiveStormItem> items)
        {
            if (items == null) return new List<ActiveStormDto>();
            return items.Select(Map).ToList();
        }
    }
}
