using Avalonia.Media;
using OmniWatch.Helpers;
using OmniWatch.Localization;
using System;
using System.ComponentModel;

namespace OmniWatch.Models.IPMA.Awarness
{
    public class AwarnessItemDto : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string Area { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // EN (from translation service)
        public string Level { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Text { get; set; } = string.Empty;

        // Brush vem do VM
        public SolidColorBrush LevelBrush { get; set; } = new SolidColorBrush(Colors.Gray);

        // ============================
        // LOCALIZED PROPERTIES
        // ============================

        public string LocalizedType =>
            LanguageManager.Instance.CurrentCulture.TwoLetterISOLanguageName switch
            {
                "pt" => WeatherTranslationService.TranslateAwarenessToPT(Type),
                _ => Type
            };

        public string LocalizedLevel =>
            LanguageManager.Instance.CurrentCulture.TwoLetterISOLanguageName switch
            {
                "pt" => WeatherTranslationService.TranslateLevelToPT(Level),
                _ => WeatherTranslationService.TranslateLevelToEN(Level)
            };

        public string Period =>
            $"{StartTime:dd/MM HH:mm} - {EndTime:dd/MM HH:mm}";

        // ============================
        // LANGUAGE CHANGE NOTIFY
        // ============================

        public void OnLanguageChanged()
        {
            PropertyChanged?.Invoke(this, new(nameof(LocalizedType)));
            PropertyChanged?.Invoke(this, new(nameof(LocalizedLevel)));
        }
    }
}
