using Avalonia.Data;

namespace OmniWatch.Localization;

public class LocalizeExtension : Binding
{
    public LocalizeExtension(string key)
        : base($"[{key}]") // Define o Path do indexador
    {
        Source = LanguageManager.Instance;
        Mode = BindingMode.OneWay;
    }
}
