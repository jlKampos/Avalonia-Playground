using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using OmniWatch.Interfaces;
using OmniWatch.ViewModels.ProgressControl;
using OmniWatch.ViewModels.Settings;
using System;
using System.Threading.Tasks;
using static OmniWatch.ViewModels.MessageDialog.MessageDialogBoxViewModel;

namespace OmniWatch.Views.Settings;

public partial class SettingsPageView : UserControl, IAsyncPage
{
    private readonly IMessageService _messageService;

    public SettingsPageView(IMessageService messageService)
    {
        _messageService = messageService;
        InitializeComponent();

    }
    public SettingsPageView()
    {
        InitializeComponent();
    }

    public Task LoadAsync()
    {
        return Task.CompletedTask;
    }

    public Task UnloadAsync()
    {
        return Task.CompletedTask;
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {

        if (DataContext is SettingsPageViewModel vm)
        {
            vm.Save();
        }

    }

    private void OnResetClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsPageViewModel vm)
            vm.Reset();
    }

    private void Numeric_ValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (sender is NumericUpDown num && num.Value is null)
            num.Value = num.Minimum;
    }


}