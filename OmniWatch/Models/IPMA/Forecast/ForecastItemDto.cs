using OmniWatch.Localization;
using OmniWatch.Models.IPMA.Awarness;
using OmniWatch.Models.IPMA.Precipitation;
using OmniWatch.Models.IPMA.Weather;
using OmniWatch.Models.IPMA.Wind;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace OmniWatch.Models.IPMA.Forecast
{
    public class ForecastItemDto : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string WeekDay { get; set; } = string.Empty;
        public int DayOfYear { get; set; }

        public string DisplayDate { get; set; } = string.Empty;
        public DateTime Date { get; set; }

        public string PrecipitationProbability { get; set; } = string.Empty;

        public string TemperatureMin { get; set; } = string.Empty;

        public string TemperatureMax { get; set; } = string.Empty;

        public string PredictedWindDirection { get; set; } = string.Empty;

        public int WeatherTypeId { get; set; }

        public int WindSpeedClass { get; set; }

        public int? PrecipitationIntensityClass { get; set; }

        public string Latitude { get; set; } = string.Empty;

        public string Longitude { get; set; } = string.Empty;

        public WeatherTypeDto WeatherInformation { get; set; } = new();

        public WindSpeedDto WindInformation { get; set; } = new();

        public List<AwarnessItemDto> AwarnessInformation { get; set; } = new();

        public PrecipitationDto PrecipitationInformation { get; set; } = new();

        public bool HasAwareness => AwarnessInformation?.Count > 0;

        // ============================================================
        // LOCALIZED PROPERTIES
        // ============================================================

        public string LocalizedWeekDay
        {
            get
            {
                var culture = LanguageManager.Instance.CurrentCulture;
                return culture.DateTimeFormat.GetDayName(Date.DayOfWeek);
            }
        }

        public string LocalizedWeatherDescription =>
            LanguageManager.Instance.CurrentCulture.TwoLetterISOLanguageName switch
            {
                "pt" => WeatherInformation?.DescriptionPT ?? "",
                _ => WeatherInformation?.DescriptionEN ?? ""
            };

        public string LocalizedWindDescription =>
            LanguageManager.Instance.CurrentCulture.TwoLetterISOLanguageName switch
            {
                "pt" => WindInformation?.DescriptionPT ?? "",
                _ => WindInformation?.DescriptionEN ?? ""
            };

        public string LocalizedPrecipitationDescription =>
            LanguageManager.Instance.CurrentCulture.TwoLetterISOLanguageName switch
            {
                "pt" => PrecipitationInformation?.DescriptionPT ?? "",
                _ => PrecipitationInformation?.DescriptionEN ?? ""
            };

        // ============================================================
        // LANGUAGE CHANGE NOTIFICATION
        // ============================================================

        public void OnLanguageChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalizedWeekDay)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalizedWeatherDescription)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalizedWindDescription)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LocalizedPrecipitationDescription)));
        }
    }
}
