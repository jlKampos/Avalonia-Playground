using AutoMapper;
using MyAvalonia.Helpers;
using MyAvalonia.Integrations.Contracts.Awarness;
using MyAvalonia.Integrations.Contracts.Forecast;
using MyAvalonia.Integrations.Contracts.Locations;
using MyAvalonia.Integrations.Contracts.Precipitation;
using MyAvalonia.Integrations.Contracts.Seismic;
using MyAvalonia.Integrations.Contracts.Weather;
using MyAvalonia.Integrations.Contracts.Wind;
using MyAvalonia.Models.Awarness;
using MyAvalonia.Models.Forecast;
using MyAvalonia.Models.Locations;
using MyAvalonia.Models.Precipitation;
using MyAvalonia.Models.Seismic;
using MyAvalonia.Models.Weather;
using MyAvalonia.Models.Wind;

namespace MyAvalonia.Mapping
{
	public class IpmaMappingProfile : Profile
	{
		public IpmaMappingProfile()
		{
			// =========================
			// LOCATION
			// =========================
			CreateMap<LocationItem, LocationDto>()
			.ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Local));

			// =========================
			// FORECAST
			// =========================
			CreateMap<ForecastItem, ForecastItemDto>()
			.ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.ForecastDate))
			.ForMember(dest => dest.DisplayDate, opt => opt.MapFrom(src => src.ForecastDate.ToString("dd/MM/yyyy")))
			.ForMember(dest => dest.WeekDay, opt => opt.MapFrom(src => src.ForecastDate.ToString("dddd", new System.Globalization.CultureInfo("en-US"))))
			.ForMember(dest => dest.DayOfYear, opt => opt.MapFrom(src => src.ForecastDate.DayOfYear))
			.ForMember(dest => dest.PrecipitationIntensityClass, opt => opt.MapFrom(src => src.PrecipitationIntensityClass));

			// =========================
			// WEATHER TYPES (DRTO)
			// =========================
			CreateMap<WeatherTypeItem, WeatherTypeDto>();

			// =========================
			// WIND SPEED
			// =========================
			CreateMap<WindSpeedItem, WindSpeedDto>();

			// =========================
			// SEISMIC ACTIVITY
			// =========================
			CreateMap<SeismicItem, SeismicActivityDto>()
				.ForMember(dest => dest.Sensed, opt => opt.MapFrom(src => src.Sensed != null ? src.Sensed.ToString() : null));

			// =========================
			// AWARENESS
			// =========================
			CreateMap<AwarenessItem, AwarnessItemDto>()
				.ForMember(dest => dest.Area, opt => opt.MapFrom(src => src.IdAreaAviso))
				.ForMember(dest => dest.Type, opt =>
					opt.MapFrom(src => WeatherTranslationService.TranslateAwareness(src.AwarenessTypeName)))
				.ForMember(dest => dest.Level, opt => opt.MapFrom(src => src.AwarenessLevelID))
				.ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime))
				.ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime))
				.ForMember(dest => dest.Text, opt => opt.MapFrom(src => src.Text));

			// =========================
			// PRECIPITATION (IPMA)
			// =========================
			CreateMap<PrecipitationItem, PrecipitationDto>()
				.ForMember(dest => dest.DescriptionEN, opt => opt.MapFrom(src => src.DescClassPrecIntEn))
				.ForMember(dest => dest.DescriptionPT, opt => opt.MapFrom(src => src.DescClassPrecIntPt))
				.ForMember(dest => dest.IntensityLevel, opt => opt.MapFrom(src => src.ClassPrecInt));
		}
	}
}
