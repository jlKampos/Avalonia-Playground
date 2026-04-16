using Avalonia.Controls;
using Mapsui.Widgets;
using Mapsui.Widgets.InfoWidgets;
using MyAvalonia.ViewModels;

namespace MyAvalonia.Views;

public partial class OepnSkyPageView : UserControl
{
    public OepnSkyPageView()
    {
        InitializeComponent();
        LoggingWidget.ShowLoggingInMap = ActiveMode.No;

        this.DataContextChanged += (s, e) =>
        {
            if (DataContext is OepnSkyPageViewModel vm)
            {
                MyMapControl.Map = vm.Map;
            }
        };
    }
}