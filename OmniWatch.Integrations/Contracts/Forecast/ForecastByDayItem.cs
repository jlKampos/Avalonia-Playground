using System.Text.Json.Serialization;

namespace OmniWatch.Integrations.Contracts.Forecast
{
    public class ForecastByDayItem
    {
        [JsonPropertyName("globalIdLocal")]
        public int GlobalIdLocal { get; set; }

        [JsonPropertyName("latitude")]
        public string Latitude { get; set; } = String.Empty;

        [JsonPropertyName("longitude")]
        public string Longitude { get; set; } = String.Empty;

        [JsonPropertyName("precipitaProb")]
        public string PrecipitationProbability { get; set; } = String.Empty;

        [JsonPropertyName("tMin")]
        public int TemperatureMin { get; set; }

        [JsonPropertyName("tMax")]
        public int TemperatureMax { get; set; }

        [JsonPropertyName("predWindDir")]
        public string PredictedWindDirection { get; set; } = String.Empty;

        [JsonPropertyName("idWeatherType")]
        public int WeatherTypeId { get; set; }

        [JsonPropertyName("classWindSpeed")]
        public int WindSpeedClass { get; set; }

        [JsonPropertyName("classPrecInt")]
        public int? PrecipitationIntensityClass { get; set; }
    }
}
