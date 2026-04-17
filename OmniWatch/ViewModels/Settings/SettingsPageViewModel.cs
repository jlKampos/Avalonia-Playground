using CommunityToolkit.Mvvm.ComponentModel;
using OmniWatch.Core.Interfaces;
using OmniWatch.Core.Settings;
using OmniWatch.Interfaces;
using OmniWatch.ViewModels.ProgressControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;
using static OmniWatch.ViewModels.MessageDialog.MessageDialogBoxViewModel;

namespace OmniWatch.ViewModels.Settings
{
    public partial class SettingsPageViewModel : PageViewModel
    {
        private readonly ISettingsService _settingsService;
        private readonly ProgressControlViewModel _progressControl;
        private readonly IMessageService _messageService;

        [ObservableProperty] private string userName;
        [ObservableProperty] private string password;
        [ObservableProperty] private int refreshInterval;
        [ObservableProperty] private bool useOpenSkyCredentials;

        public string Language { get; set; }

        public class LanguageItem
        {
            public string Code { get; set; }
            public string Name { get; set; }
        }

        private LanguageItem selectedLanguage;

        public List<LanguageItem> Languages { get; } = new()
        {
            new LanguageItem { Code = "pt-PT", Name = "Português (Portugal)" },
            new LanguageItem { Code = "en-US", Name = "English (US)" }
        };

        public LanguageItem SelectedLanguage
        {
            get => selectedLanguage;
            set
            {
                selectedLanguage = value;
                Language = value?.Code;
            }
        }


        public SettingsPageViewModel(ISettingsService settingsService, ProgressControlViewModel progressControl, IMessageService messageService)
        {
            PageName = Data.ApplicationPageNames.Settings;

            _settingsService = settingsService;
            _progressControl = progressControl;
            _messageService = messageService;

            _ = Load();
        }

        public async Task Load()
        {
            try
            {
                var settings = _settingsService.Load();

                UserName = settings.UserName;
                Password = settings.Password;
                Language = settings.Language;
                RefreshInterval = settings.RefreshInterval;
                UseOpenSkyCredentials = settings.UseOpenSkyCredentials;

                SelectedLanguage = Languages.FirstOrDefault(x => x.Code == settings.Language);
            }
            catch (Exception ex)
            {

                await _messageService.ShowAsync($"Failed to load settings: {ex.Message}", MessageDialogType.Error).ConfigureAwait(false);
            }

        }

        partial void OnUseOpenSkyCredentialsChanged(bool value)
        {
            if (!value && RefreshInterval < 10)
            {

                _ = _messageService.ShowAsync("OpenSky credentials are required for refresh intervals less than 10 seconds. Refresh interval has been reset to 10 seconds.", MessageDialogType.Warning).ConfigureAwait(false);
                RefreshInterval = 10;
            }
        }

        public void Save()
        {
            _ = SaveSettingsAsync();
        }

        private async Task SaveSettingsAsync()
        {

            try
            {
                _settingsService.Save(new AppSettings
                {
                    UserName = UserName,
                    Password = Password,
                    Language = Language,
                    RefreshInterval = RefreshInterval,
                    UseOpenSkyCredentials = UseOpenSkyCredentials
                });
            }
            catch (Exception ex)
            {

                await _messageService.ShowAsync($"Failed to save settings: {ex.Message}", MessageDialogType.Error).ConfigureAwait(false);
            }
        }
    }
}