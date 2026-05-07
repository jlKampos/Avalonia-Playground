using System.ComponentModel;
using System.Globalization;

namespace OmniWatch.Localization;

public class LanguageManager : INotifyPropertyChanged
{
    public static LanguageManager Instance { get; } = new();

    public string this[string key] =>
        UI.ResourceManager.GetString(key, CurrentCulture) ?? $"!{key}!";

    private CultureInfo _currentCulture = CultureInfo.CurrentUICulture;
    public CultureInfo CurrentCulture
    {
        get => _currentCulture;
        set
        {
            if (_currentCulture != value)
            {
                _currentCulture = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));

            }
        }
    }


    public event PropertyChangedEventHandler? PropertyChanged;
}
