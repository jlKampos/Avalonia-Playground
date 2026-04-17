using OmniWatch.Core.Interfaces;
using OmniWatch.Core.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniWatch.Core.Startup
{
    public class AppInitializer
    {
        private readonly ISettingsService _settingsService;
        private readonly ISecretService _secretService;

        public AppInitializer(
            ISettingsService settingsService,
            ISecretService secretService)
        {
            _settingsService = settingsService;
            _secretService = secretService;
        }

        public void Initialize()
        {
            InitializeSettings();
            InitializeSecrets();
        }

        private void InitializeSettings()
        {
            var settings = _settingsService.Load();

            // se não existir ficheiro ou estiver vazio, cria defaults
            if (settings == null || string.IsNullOrWhiteSpace(settings.UserName))
            {
                _settingsService.Save(new AppSettings
                {
                    UserName = "",
                    Password = "",
                    Language = "en-US"
                });
            }
        }

        private void InitializeSecrets()
        {
            var secret = _secretService.Load();

            if (string.IsNullOrEmpty(secret))
            {
                _secretService.Save("");
            }
        }
    }
}
