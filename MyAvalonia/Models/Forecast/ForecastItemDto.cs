using MyAvalonia.Models.Awarness;
using MyAvalonia.Models.Weather;
using MyAvalonia.Models.Wind;
using System;
using System.Collections.Generic;

namespace MyAvalonia.Models.Forecast
{
	public class ForecastItemDto
	{
		public string WeekDay { get; set; } = String.Empty;
		public int DayOfYear { get; set; }

		public string DisplayDate { get; set; } = String.Empty;
		public DateTime Date { get; set; }

		public string PrecipitationProbability { get; set; } = String.Empty;

		public string TemperatureMin { get; set; } = String.Empty;

		public string TemperatureMax { get; set; } = String.Empty;

		public string PredictedWindDirection { get; set; } = String.Empty;

		public int WeatherTypeId { get; set; }

		public int WindSpeedClass { get; set; }

		public int? PrecipitationIntensityClass { get; set; }

		public string Latitude { get; set; } = String.Empty;

		public string Longitude { get; set; } = String.Empty;

		public string TemperatureDisplay => $"{TemperatureMin}º / {TemperatureMax}º";

		public WeatherTypeDto WeatherInformation { get; set; } = new();

		public WindSpeedDto WindInformation { get; set; } = new();

		public List<AwarnessItemDto> AwarnessInformation { get; set; } = new();
	}
}
