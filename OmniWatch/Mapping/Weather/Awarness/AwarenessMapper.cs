using OmniWatch.Helpers;
using OmniWatch.Integrations.Contracts.Awarness;
using OmniWatch.Models.Awarness;

namespace OmniWatch.Mapping.Weather.Awarness
{
    public static class AwarenessMapper
    {
        public static AwarnessItemDto ToDto(this AwarenessItem src)
        {
            return new AwarnessItemDto
            {
                Area = src.IdAreaAviso,
                Type = WeatherTranslationService.TranslateAwareness(src.AwarenessTypeName),
                Level = src.AwarenessLevelID,
                StartTime = src.StartTime,
                EndTime = src.EndTime,
                Text = src.Text
            };
        }
    }
}
