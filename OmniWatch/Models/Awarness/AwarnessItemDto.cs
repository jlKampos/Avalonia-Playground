using Avalonia.Media;
using System;

namespace OmniWatch.Models.Awarness
{
    public class AwarnessItemDto
    {
        public string Area { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public string Text { get; set; } = string.Empty;

        public string Period => $"{StartTime:dd/MM HH:mm} - {EndTime:dd/MM HH:mm}";

        public string HasText => string.IsNullOrWhiteSpace(Text)
            ? "Sem descrição"
            : Text;

        public string DisplayLevel =>
           Level?.ToLower() switch
           {
               "green" => "Green",
               "yellow" => "Yellow",
               "orange" => "Orange",
               "red" => "Red",
               _ => Level
           };

        public SolidColorBrush LevelBrush =>
            Level?.ToLower() switch
            {
                "green" => new SolidColorBrush(Colors.Green),
                "yellow" => new SolidColorBrush(Colors.Yellow),
                "orange" => new SolidColorBrush(Colors.Orange),
                "red" => new SolidColorBrush(Colors.Red),
                _ => new SolidColorBrush(Colors.Gray)
            };
    }
}
