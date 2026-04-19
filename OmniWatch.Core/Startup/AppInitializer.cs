using OmniWatch.Core.Interfaces;
using OmniWatch.Core.Settings;

namespace OmniWatch.Core.Startup
{
    public class AppInitializer
    {
        #region Fields

        private readonly ISettingsService _settingsService;
        private readonly ISecretService _secretService;

        #endregion

        #region Constructor

        public AppInitializer(
            ISettingsService settingsService,
            ISecretService secretService)
        {
            _settingsService = settingsService;
            _secretService = secretService;
        }

        #endregion

        #region Public API

        public void Initialize()
        {
            InitializeSettings();
            InitializeSecrets();
        }

        #endregion

        #region Internal Logic

        private void InitializeSettings()
        {
            var settings = _settingsService.Load();

            // If file missing or invalid → create defaults
            if (settings == null)
            {
                _settingsService.Save(new AppSettings
                {
                    UseOpenSkyCredentials = false,
                    OpenSkyClientId = "",
                    RefreshInterval = 10,
                    Language = "en-US"
                });
            }
        }

        private void InitializeSecrets()
        {
            // Secret file stores ONLY the OpenSkyClientSecret
            var secret = _secretService.Load();

            if (string.IsNullOrEmpty(secret))
            {
                // Save empty encrypted secret
                _secretService.Save("");
            }
        }

        #endregion
    }
}
