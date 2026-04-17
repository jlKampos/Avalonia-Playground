using System;
using System.Collections.Generic;

namespace OmniWatch.Helpers
{
    public static class WeatherTranslationService
    {
        private static readonly Dictionary<string, string> AwarenessTypes = new(StringComparer.OrdinalIgnoreCase)
        {
			// Hidrometeorológicos
			{ "Agitação Marítima", "Sea Swell" },
            { "Trovoada", "Thunderstorms" },
            { "Precipitação", "Rain / Precipitation" },
            { "Chuva", "Rain" },
            { "Vento", "Wind" },
            { "Vento Forte", "Strong Wind" },
            { "Nevoeiro", "Fog" },
            { "Neve", "Snow" },
        
			// Temperaturas e Clima
			{ "Tempo Frio", "Cold Weather" },
            { "Tempo Quente", "Hot Weather" },
            { "Onda de Calor", "Heatwave" },
            { "Onda de Frio", "Cold Wave" },
        
			// Riscos Ambientais
			{ "Incêndio", "Fire Risk" },
            { "Risco de Incêndio", "Fire Risk" },
            { "Incêndio Rural", "Rural Fire Risk" },
            { "Radiação UV", "UV Radiation" },
            { "Poeiras", "Dust / Sand in suspension" },

			// Níveis de Aviso (Se precisares de traduzir o levelID)
			{ "green", "Low Risk" },
            { "yellow", "Moderate Risk" },
            { "orange", "High Risk" },
            { "red", "Extreme Risk" }
        };


        public static string TranslateAwareness(string ptTerm)
        {
            if (string.IsNullOrWhiteSpace(ptTerm))
                return "General Alert";

            // Remove espaços em branco extras que a API possa enviar
            var cleanedTerm = ptTerm.Trim();

            if (AwarenessTypes.TryGetValue(cleanedTerm, out var enTerm))
            {
                return enTerm;
            }

            // Caso o termo seja novo, fazemos um log (opcional) e devolvemos o original
            System.Diagnostics.Debug.WriteLine($"[Translation] Missing term: {cleanedTerm}");
            return cleanedTerm;
        }
    }
}
