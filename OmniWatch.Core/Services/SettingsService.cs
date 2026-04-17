using OmniWatch.Core.Interfaces;
using OmniWatch.Core.Settings;
using System.Text.Json;

namespace OmniWatch.Core.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly string _filePath;

        public SettingsService()
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OmniWatch");

            Directory.CreateDirectory(folder);
            _filePath = Path.Combine(folder, "settings.json");
        }

        public AppSettings Load()
        {
            if (!File.Exists(_filePath))
                return new AppSettings
                {
                    UserName = "",
                    Password = "",
                    Language = "en-US"
                };

            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_filePath))
                   ?? new AppSettings();
        }

        public void Save(AppSettings settings)
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_filePath, json);
        }
    }
}
