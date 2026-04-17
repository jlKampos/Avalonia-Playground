using Avalonia;
using Avalonia.Controls;
using OmniWatch.ViewModels;

namespace OmniWatch.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
#if DEBUG
            this.AttachDevTools();
#endif
            InitializeComponent();
        }

        private void Image_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            ((MainWindowViewModel)DataContext).SideMenuResizeCommand?.Execute(null);
        }

    }
}