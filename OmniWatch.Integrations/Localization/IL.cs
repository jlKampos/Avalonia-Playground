using System.Globalization;
using System.Resources;

namespace OmniWatch.Integrations.Localization
{
    public static class IL
    {
        private static readonly ResourceManager _rm =
            new ResourceManager("OmniWatch.Integrations.Resources.Integrations", typeof(IL).Assembly);

        public static string Translation(string key) =>
            _rm.GetString(key, CultureInfo.CurrentUICulture) ?? key;
    }
}
