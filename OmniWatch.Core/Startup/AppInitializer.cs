using OmniWatch.Core.Enums;
using OmniWatch.Core.Helpers;
using OmniWatch.Core.Interfaces;
using OmniWatch.Core.Settings;

namespace OmniWatch.Core.Startup
{
    public class AppInitializer
    {
        #region Fields

        private readonly ISettingsService _settingsService;
        private readonly ISecretService _secretService;
        private readonly ILocalizationService _localizationService;
        #endregion

        #region Constructor

        public AppInitializer(
            ISettingsService settingsService,
            ISecretService secretService,
            ILocalizationService localizationService)
        {
            _settingsService = settingsService;
            _secretService = secretService;
            _localizationService = localizationService;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Blocking initialization (safe for app startup)
        /// </summary>
        public void Initialize()
        {
            InitializeSettings();
            InitializeSecrets()
                .GetAwaiter()
                .GetResult(); // ensures deterministic startup
        }

        #endregion

        #region Internal Logic

        private void InitializeSettings()
        {
            var settings = _settingsService.Load();

            // If file missing or invalid → create defaults
            if (settings == null)
            {
                settings = new AppSettings
                {
                    UseOpenSkyCredentials = false,
                    OpenSkyClientId = string.Empty,
                    RefreshInterval = 10,
                    Language = "en-US"
                };

                _settingsService.Save(settings);
            }

            _localizationService.SetCulture(settings.Language);

        }

        private async Task InitializeSecrets()
        {
            var key = SecretKeys.ApiKey(ApiProvider.OpenSky);

            var secret = await _secretService
                .GetAsync(key)
                .ConfigureAwait(false);

            if (secret is null)
            {
                await _secretService
                    .SetAsync(key, string.Empty)
                    .ConfigureAwait(false);
            }
        }

        #endregion
    }
}