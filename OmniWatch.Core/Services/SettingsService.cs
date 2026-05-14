using OmniWatch.Core.Interfaces;
using OmniWatch.Core.Settings;
using System.Text.Json;

namespace OmniWatch.Core.Services;

public class SettingsService : ISettingsService
{
    private readonly string _filePath;

    public event Action<AppSettings>? SettingsChanged;

    public SettingsService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "OmniWatch");

        Directory.CreateDirectory(folder);

        _filePath = Path.Combine(folder, "settings.json");
    }

    internal SettingsService(string filePath)
    {
        _filePath = filePath;
    }

    public AppSettings Load()
    {
        if (!File.Exists(_filePath))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(_filePath);

            return JsonSerializer.Deserialize<AppSettings>(json)
                   ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        File.WriteAllText(_filePath, json);

        SettingsChanged?.Invoke(settings);
    }
}