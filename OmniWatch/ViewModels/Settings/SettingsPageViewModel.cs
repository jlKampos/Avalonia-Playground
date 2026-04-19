using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OmniWatch.Core.Interfaces;
using OmniWatch.Core.Settings;
using OmniWatch.Integrations.Enums;
using OmniWatch.Integrations.Interfaces;
using OmniWatch.Interfaces;
using OmniWatch.ViewModels.MessageDialog;
using OmniWatch.ViewModels.ProgressControl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static OmniWatch.ViewModels.MessageDialog.MessageDialogBoxViewModel;

namespace OmniWatch.ViewModels.Settings
{
    public partial class SettingsPageViewModel : PageViewModel
    {
        #region Fields

        private readonly ISettingsService _settingsService;
        private readonly ISecretService _secretService;
        private readonly IMessageService _messageService;
        private readonly IOpenSkyTokenManager _tokenManager;

        #endregion

        #region Observable Properties

        [ObservableProperty]
        private ProgressControlViewModel _progressControl = new();

        [ObservableProperty]
        private string _openSkyClientId;

        [ObservableProperty]
        private string _openSkyClientSecret;

        [ObservableProperty]
        private string _language;

        [ObservableProperty]
        private int _refreshInterval;

        [ObservableProperty]
        private bool _useOpenSkyCredentials = false;

        [ObservableProperty]
        private bool _isSecretVisible;

        public string SecretVisibilityIcon => IsSecretVisible ? "\uE224" : "\uE220";

        #endregion

        #region Language List

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

        #endregion

        #region Constructor

        public SettingsPageViewModel(
            ISettingsService settingsService,
            ISecretService secretService,
            ProgressControlViewModel progressControl,
            IMessageService messageService,
            IOpenSkyTokenManager tokenManager)
        {
            PageName = Data.ApplicationPageNames.Settings;

            _tokenManager = tokenManager;
            _secretService = secretService;
            _settingsService = settingsService;
            _messageService = messageService;
            ProgressControl = progressControl;

            _ = Load();
        }

        #endregion

        #region Load

        public async Task Load()
        {
            try
            {
                ProgressControl.IsVisible = true;
                ProgressControl.Message = "Loading settings...";

                var settings = _settingsService.Load();
                var secret = _secretService.Load();

                OpenSkyClientId = settings.OpenSkyClientId ?? "";
                OpenSkyClientSecret = secret ?? "";
                Language = settings.Language;
                RefreshInterval = settings.RefreshInterval;
                UseOpenSkyCredentials = settings.UseOpenSkyCredentials;

                SelectedLanguage = Languages.FirstOrDefault(x => x.Code == settings.Language);
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync(
                    $"Failed to load settings: {ex.Message}",
                    MessageDialogType.Error
                ).ConfigureAwait(false);
            }
            finally
            {
                ProgressControl.IsVisible = false;
            }
        }

        #endregion

        #region Commands

        public IRelayCommand ToggleSecretVisibilityCommand => new RelayCommand(() => IsSecretVisible = !IsSecretVisible);


        partial void OnIsSecretVisibleChanged(bool value)
        {
            OnPropertyChanged(nameof(SecretVisibilityIcon));
        }


        [RelayCommand]
        private async Task TestOpenSkyCredentialsAsync()
        {
            try
            {
                ProgressControl.IsVisible = true;
                ProgressControl.Message = "Validating OpenSky credentials...";

                // Testar credenciais diretamente (sem gravar)
                var result = await _tokenManager.TestCredentialsAsync(
                    OpenSkyClientId,
                    OpenSkyClientSecret
                );

                switch (result.Status)
                {
                    case OpenSkyAuthStatus.Success:
                        await _messageService.ShowAsync(
                            "Credentials are valid!",
                            MessageDialogType.Success);
                        break;

                    case OpenSkyAuthStatus.Unauthorized:
                        await _messageService.ShowAsync(
                            "Invalid credentials.\nPlease check your Client ID and Secret.",
                            MessageDialogType.Error);
                        break;

                    case OpenSkyAuthStatus.Error:
                        await _messageService.ShowAsync(
                            "OpenSky server returned an error.\nTry again later.",
                            MessageDialogType.Error);
                        break;
                }
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync(
                    $"Unexpected error: {ex.Message}",
                    MessageDialogType.Error);
            }
            finally
            {
                ProgressControl.IsVisible = false;
            }
        }


        #endregion

        #region Validation

        partial void OnUseOpenSkyCredentialsChanged(bool value)
        {
            if (!value && RefreshInterval < 10)
            {
                _ = _messageService.ShowAsync(
                    "OpenSky credentials are required for refresh intervals less than 10 seconds. Refresh interval has been reset to 10 seconds.",
                    MessageDialogType.Warning
                ).ConfigureAwait(false);

                RefreshInterval = 10;
            }
        }

        #endregion

        #region Save

        public void Save()
        {
            _ = SaveSettingsAsync();
        }

        public void Reset()
        {
            _ = ResetSettingsAsync();
        }


        private async Task SaveSettingsAsync()
        {
            try
            {
                ProgressControl.IsVisible = true;
                ProgressControl.Message = "Saving settings...";

                if (!UseOpenSkyCredentials && RefreshInterval < 10)
                {
                    await _messageService.ShowAsync(
                        "Settings not saved.\nWarning: OpenSky requests without authentication must not be made more frequently than every 10 seconds, or your IP may be blocked.",
                        MessageDialogType.Warning
                    ).ConfigureAwait(false);

                    return;
                }

                // Save non-sensitive settings
                _settingsService.Save(new AppSettings
                {
                    UseOpenSkyCredentials = UseOpenSkyCredentials,
                    OpenSkyClientId = OpenSkyClientId,
                    RefreshInterval = RefreshInterval,
                    Language = Language
                });

                // Save sensitive secret encrypted
                _secretService.Save(OpenSkyClientSecret ?? "");

                await _messageService.ShowAsync(
                    "Settings saved successfully.",
                    MessageDialogType.Warning
                ).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync(
                    $"Failed to save settings: {ex.Message}",
                    MessageDialogType.Error
                ).ConfigureAwait(false);
            }
            finally
            {
                ProgressControl.IsVisible = false;
            }
        }

        public async Task ResetSettingsAsync()
        {
            try
            {
                var result = await _messageService.ShowAsync(
                    "Are you sure you want to reset all settings?\nThis will remove your OpenSky credentials and restore defaults.",
                    MessageDialogType.Warning
                );

                if (result != MessageDialogResult.Ok)
                    return;

                ProgressControl.IsVisible = true;
                ProgressControl.Message = "Resetting settings...";

                // Delete settings.json
                var folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "OmniWatch");

                var settingsPath = Path.Combine(folder, "settings.json");
                var secretPath = Path.Combine(folder, "secret.dat");

                if (File.Exists(settingsPath))
                    File.Delete(settingsPath);

                if (File.Exists(secretPath))
                    File.Delete(secretPath);

                // Recreate defaults
                _settingsService.Save(new AppSettings
                {
                    UseOpenSkyCredentials = false,
                    OpenSkyClientId = "",
                    RefreshInterval = 10,
                    Language = "en-US"
                });

                _secretService.Save("");

                // Reload UI
                await Load();

                await _messageService.ShowAsync(
                    "Settings have been reset successfully.",
                    MessageDialogType.Warning
                );
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync(
                    $"Failed to reset settings: {ex.Message}",
                    MessageDialogType.Error
                );
            }
            finally
            {
                ProgressControl.IsVisible = false;
            }
        }


        #endregion
    }
}
