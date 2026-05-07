using System;
using System.Collections.Generic;

namespace OmniWatch.Helpers
{
    public static class WeatherTranslationService
    {
        private static readonly Dictionary<string, string> AwarenessPTtoEN = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Agitação Marítima", "Sea Swell" },
            { "Trovoada", "Thunderstorms" },
            { "Precipitação", "Rain / Precipitation" },
            { "Chuva", "Rain" },
            { "Vento", "Wind" },
            { "Vento Forte", "Strong Wind" },
            { "Nevoeiro", "Fog" },
            { "Neve", "Snow" },
            { "Tempo Frio", "Cold Weather" },
            { "Tempo Quente", "Hot Weather" },
            { "Onda de Calor", "Heatwave" },
            { "Onda de Frio", "Cold Wave" },
            { "Incêndio", "Fire Risk" },
            { "Risco de Incêndio", "Fire Risk" },
            { "Incêndio Rural", "Rural Fire Risk" },
            { "Radiação UV", "UV Radiation" },
            { "Poeiras", "Dust / Sand in suspension" }
        };

        private static readonly Dictionary<string, string> AwarenessENtoPT = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Sea Swell", "Agitação Marítima" },
            { "Thunderstorms", "Trovoada" },
            { "Rain / Precipitation", "Precipitação" },
            { "Rain", "Chuva" },
            { "Wind", "Vento" },
            { "Strong Wind", "Vento Forte" },
            { "Fog", "Nevoeiro" },
            { "Snow", "Neve" },
            { "Cold Weather", "Tempo Frio" },
            { "Hot Weather", "Tempo Quente" },
            { "Heatwave", "Onda de Calor" },
            { "Cold Wave", "Onda de Frio" },
            { "Fire Risk", "Risco de Incêndio" },
            { "Rural Fire Risk", "Incêndio Rural" },
            { "UV Radiation", "Radiação UV" },
            { "Dust / Sand in suspension", "Poeiras" }
        };

        private static readonly Dictionary<string, string> LevelENtoPT = new(StringComparer.OrdinalIgnoreCase)
        {
            { "green", "Verde" },
            { "yellow", "Amarelo" },
            { "orange", "Laranja" },
            { "red", "Vermelho" }
        };

        private static readonly Dictionary<string, string> LevelPTtoEN = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Verde", "Green" },
            { "Amarelo", "Yellow" },
            { "Laranja", "Orange" },
            { "Vermelho", "Red" }
        };

        public static string TranslateAwareness(string ptTerm)
        {
            if (string.IsNullOrWhiteSpace(ptTerm))
                return "General Alert";

            var cleaned = ptTerm.Trim();

            return AwarenessPTtoEN.TryGetValue(cleaned, out var en)
                ? en
                : cleaned;
        }

        public static string TranslateAwarenessToPT(string enTerm)
        {
            if (string.IsNullOrWhiteSpace(enTerm))
                return "Alerta Geral";

            var cleaned = enTerm.Trim();

            return AwarenessENtoPT.TryGetValue(cleaned, out var pt)
                ? pt
                : cleaned;
        }

        public static string TranslateLevelToPT(string level)
        {
            if (LevelENtoPT.TryGetValue(level, out var pt))
                return pt;

            return level;
        }

        public static string TranslateLevelToEN(string level)
        {
            if (LevelPTtoEN.TryGetValue(level, out var en))
                return en;

            return level;
        }
    }
}
