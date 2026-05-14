using System.Globalization;

namespace OmniWatch.Core.Interfaces;

public interface ILocalizationService
{
    CultureInfo CurrentCulture { get; }

    void SetCulture(string culture);
}