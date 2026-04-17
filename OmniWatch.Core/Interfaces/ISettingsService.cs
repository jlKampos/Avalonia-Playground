using OmniWatch.Core.Settings;

namespace OmniWatch.Core.Interfaces
{
    public interface ISettingsService
    {
        AppSettings Load();
        void Save(AppSettings settings);
    }
}
