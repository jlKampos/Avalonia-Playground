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
        [ObservableProperty]
        private ProgressControlViewModel _progressControl = new();
        private readonly IMessageService _messageService;

        [ObservableProperty]
        private string _userName;

        [ObservableProperty]
        private string _password;

        [ObservableProperty]
        private string _language;

        [ObservableProperty]
        private int _refreshInterval;

        [ObservableProperty]
        private bool _useOpenSkyCredentials;

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
            ProgressControl = progressControl;
            _messageService = messageService;

            _ = Load();
        }

        public async Task Load()
        {
            try
            {
                ProgressControl.IsVisible = true;
                ProgressControl.Message = "Loading settings...";

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
            finally
            {

                ProgressControl.IsVisible = false;
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
                ProgressControl.IsVisible = true;
                ProgressControl.Message = "Saving settings...";

                if (!UseOpenSkyCredentials && RefreshInterval < 10)
                {

                    await _messageService.ShowAsync("Settings not saved .\nWarning: OpenSky requests without authentication must not be made more frequently than every 10 seconds, or your IP may be blocked.", MessageDialogType.Warning).ConfigureAwait(false);
                    return;
                }


                _settingsService.Save(new AppSettings
                {
                    UserName = UserName,
                    Password = Password,
                    Language = Language,
                    RefreshInterval = RefreshInterval,
                    UseOpenSkyCredentials = UseOpenSkyCredentials
                });

                await _messageService.ShowAsync("Settings saved successfully.\nNote: Some settings are not active yet and will be implemented later.", MessageDialogType.Warning).ConfigureAwait(false);
            }
            catch (Exception ex)
            {

                await _messageService.ShowAsync($"Failed to save settings: {ex.Message}", MessageDialogType.Error).ConfigureAwait(false);
            }
            finally
            {
                ProgressControl.IsVisible = false;
            }
        }
    }
}