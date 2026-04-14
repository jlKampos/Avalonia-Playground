using Avalonia.Controls;
using MyAvalonia.ViewModels;

namespace MyAvalonia.Views;

public partial class OepnSkyPageView : UserControl
{
    public OepnSkyPageView()
    {
        InitializeComponent();

        this.DataContextChanged += (s, e) =>
        {
            if (DataContext is OepnSkyPageViewModel vm)
            {
                MyMapControl.Map = vm.Map;
            }
        };
    }
}