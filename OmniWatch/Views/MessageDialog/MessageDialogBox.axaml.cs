using Avalonia.Controls;
using Avalonia.Interactivity;
using OmniWatch.ViewModels.MessageDialog;

namespace OmniWatch.Views.MessageDialog;

public partial class MessageDialogBox : Window
{

    public MessageDialogBox() => InitializeComponent();

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close(MessageDialogResult.Ok);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(MessageDialogResult.Cancel);
    }


}