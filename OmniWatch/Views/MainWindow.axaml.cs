using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using OmniWatch.ViewModels;
using System;

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

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
        }

        private void Image_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.SideMenuResizeCommand?.Execute(null);
            }
        }
    }
}