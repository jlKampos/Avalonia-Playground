using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using OmniWatch.ViewModels;
using System;
using System.Threading.Tasks;

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

        protected override async void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            var initializer = App.Current.ServiceProvider
                .GetRequiredService<OmniWatch.Core.Startup.AppInitializer>();

            // corre sem bloquear UI
            await Task.Run(() => initializer.Initialize());
        }

        private void Image_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            ((MainWindowViewModel)DataContext).SideMenuResizeCommand?.Execute(null);
        }
    }
}