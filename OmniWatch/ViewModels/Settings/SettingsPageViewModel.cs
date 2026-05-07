using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HarfBuzzSharp;
using OmniWatch.Core.Enums;
using OmniWatch.Core.Helpers;
using OmniWatch.Core.Interfaces;
using OmniWatch.Core.Settings;
using OmniWatch.Integrations.Enums;
using OmniWatch.Integrations.Interfaces;
using OmniWatch.Interfaces;
using OmniWatch.Localization;
using OmniWatch.ViewModels.MessageDialog;
using OmniWatch.ViewModels.ProgressControl;
using System;
using System.Collections.Generic;
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
        private readonly ISecretResetService _secretResetService;
        private readonly IMessageService _messageService;
        private readonly IOpenSkyTokenManager _tokenManager;

        #endregion

        #region Localization Helper

        private string Translation(string key) =>
            LanguageManager.Instance[key];

        #endregion

        #region Observable Properties

        [ObservableProperty]
        private ProgressControlViewModel _progressControl;

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
                if (SetProperty(ref selectedLanguage, value) && value != null)
                {
                    Language = value.Code;

                    LanguageManager.Instance.CurrentCulture =
                        new System.Globalization.CultureInfo(value.Code);
                }
            }
        }

        #endregion

        #region Constructor

        public SettingsPageViewModel(
            ISettingsService settingsService,
            ISecretService secretService,
            ISecretResetService secretResetService,
            ProgressControlViewModel progressControl,
            IMessageService messageService,
            IOpenSkyTokenManager tokenManager) : base()
        {
            PageName = Data.ApplicationPageNames.Settings;

            _tokenManager = tokenManager;
            _secretService = secretService;
            _settingsService = settingsService;
            _secretResetService = secretResetService;
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
                ProgressControl.Message = Translation("Settings_Loading");

                var settings = _settingsService.Load();
                var secret = await _secretService.GetAsync(SecretKeys.ApiKey(ApiProvider.OpenSky));

                OpenSkyClientId = settings.OpenSkyClientId ?? string.Empty;
                OpenSkyClientSecret = secret ?? string.Empty;

                Language = settings.Language;
                RefreshInterval = settings.RefreshInterval;
                UseOpenSkyCredentials = settings.UseOpenSkyCredentials;

                SelectedLanguage = Languages.FirstOrDefault(x => x.Code == settings.Language);
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync(
                    string.Format(Translation("Settings_LoadFailed"), ex.Message),
                    MessageDialogType.Error
                );
            }
            finally
            {
                ProgressControl.IsVisible = false;
            }
        }

        #endregion

        #region Commands

        public IRelayCommand ToggleSecretVisibilityCommand =>
            new RelayCommand(() => IsSecretVisible = !IsSecretVisible);

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
                ProgressControl.Message = Translation("Settings_ValidatingCredentials");

                var result = await _tokenManager.TestCredentialsAsync(
                    OpenSkyClientId,
                    OpenSkyClientSecret
                );

                switch (result.Status)
                {
                    case OpenSkyAuthStatus.Success:
                        await _messageService.ShowAsync(
                            Translation("Settings_CredentialsValid"),
                            MessageDialogType.Success);
                        break;

                    case OpenSkyAuthStatus.Unauthorized:
                        await _messageService.ShowAsync(
                            Translation("Settings_CredentialsInvalid"),
                            MessageDialogType.Error);
                        break;

                    case OpenSkyAuthStatus.Error:
                        await _messageService.ShowAsync(
                            Translation("Settings_ServerError"),
                            MessageDialogType.Error);
                        break;
                }
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync(
                    string.Format(Translation("Settings_UnexpectedError"), ex.Message),
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
                    Translation("Settings_RefreshIntervalWarning"),
                    MessageDialogType.Warning
                ).ConfigureAwait(false);

                RefreshInterval = 10;
            }
        }

        #endregion

        #region Save

        public void Save() => _ = SaveSettingsAsync();
        public void Reset() => _ = ResetSettingsAsync();

        private async Task SaveSettingsAsync()
        {
            try
            {
                ProgressControl.IsVisible = true;
                ProgressControl.Message = Translation("Settings_Saving");

                if (!UseOpenSkyCredentials && RefreshInterval < 10)
                {
                    await _messageService.ShowAsync(
                        Translation("Settings_SaveBlockedNoAuth"),
                        MessageDialogType.Warning
                    );
                    RefreshInterval = 10;
                    return;
                }
                else if (UseOpenSkyCredentials &&
                        (string.IsNullOrWhiteSpace(OpenSkyClientId) ||
                         string.IsNullOrWhiteSpace(OpenSkyClientSecret)))
                {
                    await _messageService.ShowAsync(
                        Translation("Settings_SaveBlockedMissingCredentials"),
                        MessageDialogType.Warning
                    );
                    return;
                }

                _settingsService.Save(new AppSettings
                {
                    UseOpenSkyCredentials = UseOpenSkyCredentials,
                    OpenSkyClientId = OpenSkyClientId,
                    RefreshInterval = RefreshInterval,
                    Language = Language
                });

                if (!string.IsNullOrWhiteSpace(OpenSkyClientSecret))
                {
                    await _secretService.SetAsync(
                        SecretKeys.ApiKey(ApiProvider.OpenSky),
                        OpenSkyClientSecret
                    );
                }

                await _messageService.ShowAsync(
                    Translation("Settings_SaveSuccess"),
                    MessageDialogType.Warning
                );
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync(
                    string.Format(Translation("Settings_SaveFailed"), ex.Message),
                    MessageDialogType.Error);
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
                    Translation("Settings_ResetConfirm"),
                    MessageDialogType.Warning
                );

                if (result != MessageDialogResult.Ok)
                    return;

                ProgressControl.IsVisible = true;
                ProgressControl.Message = Translation("Settings_Resetting");

                _settingsService.Save(new AppSettings
                {
                    UseOpenSkyCredentials = false,
                    OpenSkyClientId = string.Empty,
                    RefreshInterval = 10,
                    Language = "en-US"
                });

                await _secretResetService.ResetAsync(ApiProvider.OpenSky);

                await Load();

                await _messageService.ShowAsync(
                    Translation("Settings_ResetSuccess"),
                    MessageDialogType.Warning
                );
            }
            catch (Exception ex)
            {
                await _messageService.ShowAsync(
                    string.Format(Translation("Settings_ResetFailed"), ex.Message),
                    MessageDialogType.Error);
            }
            finally
            {
                ProgressControl.IsVisible = false;
            }
        }

        #endregion
    }
}
