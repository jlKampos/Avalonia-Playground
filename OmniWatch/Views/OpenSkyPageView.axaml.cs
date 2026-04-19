using Avalonia;
using Avalonia.Controls;
using Mapsui.Widgets;
using Mapsui.Widgets.InfoWidgets;
using OmniWatch.ViewModels;

namespace OmniWatch.Views;

public partial class OpenSkyPageView : UserControl
{
    public OpenSkyPageView()
    {
        InitializeComponent();
        LoggingWidget.ShowLoggingInMap = ActiveMode.No;

        this.DataContextChanged += (s, e) =>
        {
            if (DataContext is OpenSkyPageViewModel vm)
            {
                MyMapControl.Map = vm.Map;
            }
        };
    }
}