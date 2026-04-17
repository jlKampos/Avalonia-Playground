using Avalonia.Controls;
using Avalonia.Interactivity;

namespace OmniWatch.Views.MessageDialog;

public partial class MessageDialogBox : Window
{

    public MessageDialogBox() => InitializeComponent();
    private void Close_Click(object? sender, RoutedEventArgs e) => Close();

}