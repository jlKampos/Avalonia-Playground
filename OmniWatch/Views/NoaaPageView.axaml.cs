using Avalonia.Controls;
using Mapsui.Widgets;
using Mapsui.Widgets.InfoWidgets;
using OmniWatch.ViewModels;

namespace OmniWatch.Views;

public partial class NoaaPageView : UserControl
{
    public NoaaPageView()
    {
        InitializeComponent();
        LoggingWidget.ShowLoggingInMap = ActiveMode.No;

        // Link the ViewModel's Map object to the View's MapControl
        this.DataContextChanged += (s, e) =>
        {
            if (DataContext is NoaaPageViewModel vm)
            {
                MyMapControl.Map = vm.Map;
            }
        };
    }
}