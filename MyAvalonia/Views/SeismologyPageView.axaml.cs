using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapsui.Tiling;
using MyAvalonia.ViewModels;
using MyAvalonia.ViewModels.ProgressControl;
using System.Threading.Tasks;

namespace MyAvalonia.Views;

public partial class SeismologyPageView : UserControl
{

	public SeismologyPageView()
	{
		InitializeComponent();

		// Link the ViewModel's Map object to the View's MapControl
		this.DataContextChanged += (s, e) =>
		{
			if (DataContext is SeismologyPageViewModel vm)
			{
				MyMapControl.Map = vm.Map;
			}
		};
	}

}