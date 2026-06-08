using Avalonia;
using Avalonia.Controls;
using Mapsui;
using Mapsui.Projections;
using Mapsui.UI.Avalonia;
using Mapsui.Widgets;
using Mapsui.Widgets.InfoWidgets;
using OmniWatch.Models.Noaa.ActiveStorms;
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
                vm.StormSelected += (s, storm) =>
                {
                    CenterMapOnStorm(storm);
                };
            }
        };
    }

    private void CenterMapOnStorm(ActiveStormDto storm)
    {
        if (storm == null) return;

        var (x, y) = SphericalMercator.FromLonLat(storm.Longitude, storm.Latitude);

        MyMapControl.Map.Navigator.CenterOn(new MPoint(x, y));
        MyMapControl.Map.Navigator.ZoomTo(3500, duration: 300);
    }
}