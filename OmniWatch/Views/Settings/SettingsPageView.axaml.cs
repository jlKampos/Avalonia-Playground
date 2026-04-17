using Avalonia.Controls;
using Avalonia.Interactivity;
using OmniWatch.ViewModels.Settings;

namespace OmniWatch.Views.Settings;

public partial class SettingsPageView : UserControl
{
    public SettingsPageView()
    {
        InitializeComponent();
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsPageViewModel vm)
        {
            vm.Save();
        }

    }
}