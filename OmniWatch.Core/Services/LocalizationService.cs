using OmniWatch.Core.Interfaces;
using System.Globalization;

namespace OmniWatch.Core.Services;

public class LocalizationService : ILocalizationService
{
    public CultureInfo CurrentCulture =>
        CultureInfo.CurrentUICulture;

    public void SetCulture(string culture)
    {
        var ci = new CultureInfo(culture);

        CultureInfo.DefaultThreadCurrentCulture = ci;
        CultureInfo.DefaultThreadCurrentUICulture = ci;

        CultureInfo.CurrentCulture = ci;
        CultureInfo.CurrentUICulture = ci;
    }
}