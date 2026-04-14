using AutoMapper;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using MyAvalonia.Data;
using MyAvalonia.Integrations.Interfaces;
using MyAvalonia.Interfaces;
using MyAvalonia.Models.Seismic;
using MyAvalonia.ViewModels.ProgressControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static MyAvalonia.ViewModels.MessageDialog.MessageDialogBoxViewModel;

namespace MyAvalonia.ViewModels
{
	public partial class SeismologyPageViewModel : PageViewModel
	{
		private readonly IMapper _mapper;
		private readonly IIpmaService _apiClient;
		private readonly IMessageService _messageService;

		[ObservableProperty]
		private Mapsui.Map _map;

		[ObservableProperty]
		private DateTime _maxDate = DateTime.Now;

		[ObservableProperty]
		private DateTime _minDate = DateTime.Now.AddDays(-30);

		[ObservableProperty]
		private DateTime _selectedDate = DateTime.Now;

		[ObservableProperty]
		private ProgressControlViewModel _progressControl = new();

		private List<SeismicActivityDto> SeismicActivities { get; set; } = new();

		public SeismologyPageViewModel(ProgressControlViewModel progressControl, IMessageService messageService, IIpmaService apiClient, IMapper mapper)
		{
			PageName = ApplicationPageNames.Seismology;
			_progressControl = progressControl;
			_messageService = messageService;
			_mapper = mapper;
			_apiClient = apiClient;

			// Initialize map object immediately
			Map = new Mapsui.Map();
			_ = InitializeAsync();
		}

		public SeismologyPageViewModel()
		{
			if (Design.IsDesignMode)
			{
				Map = new Mapsui.Map();
				PageName = ApplicationPageNames.Seismology;
			}
		}

		private async Task InitializeAsync()
		{
			try
			{
				ProgressControl.IsVisible = true;
				ProgressControl.Title = "Loading";
				ProgressControl.Message = "Initialising seismic data...";

				await InitializeMapAsync();
				await LoadSeismologyDataAsync();

				UpdateMapFilter();
			}
			catch (Exception ex)
			{
				await _messageService.ShowAsync($"Startup Error: {ex.Message}", MessageDialogType.Error);
			}
			finally
			{
				ProgressControl.IsVisible = false;
			}
		}

		//private async Task InitializeAsync()
		//{
		//	try
		//	{
		//		ProgressControl.IsVisible = true;
		//		ProgressControl.Title = "Loading";
		//		ProgressControl.Message = "Initialising seismic data...";

		//		await InitializeMapAsync();
		//		await LoadSeismologyDataAsync();

		//		// 3. Add points to the map
		//		// TEST LOGIC: Find the most recent date that actually HAS valid earthquakes (magnitude > 0)
		//		var latesteismicActivities = SeismicActivities
		//			.Where(x => x.MagnitudeValue > 0)
		//			.OrderByDescending(x => x.Time).ToList();


		//		// Fallback: show everything without date filter if no valid magnitudes are found
		//		AddSeismicLayerToMap(latesteismicActivities);

		//	}
		//	catch (Exception ex)
		//	{
		//		await _messageService.ShowAsync($"Startup Error: {ex.Message}", MessageDialogType.Error);
		//	}
		//	finally
		//	{
		//		ProgressControl.IsVisible = false;
		//	}
		//}

		private async Task InitializeMapAsync()
		{
			try
			{
				var osmLayer = await Task.Run(() => Mapsui.Tiling.OpenStreetMap.CreateTileLayer());
				osmLayer.Name = "OSM";
				Map.Layers.Add(osmLayer);

				// Center on Portugal
				var portugalCenter = new Mapsui.MPoint(-770000, 4780000);
				Map.Navigator.CenterOnAndZoomTo(portugalCenter, Map.Navigator.Resolutions[7]);

				// Limits
				var portugalExtent = new Mapsui.MRect(-1500000, 4200000, -300000, 5400000);
				Map.Navigator.OverridePanBounds = portugalExtent;

				Map.RefreshGraphics();
			}
			catch (Exception ex)
			{
				await _messageService.ShowAsync($"Map Error: {ex.Message}", MessageDialogType.Error);
			}
		}

		private void AddSeismicLayerToMap(List<SeismicActivityDto> events, DateTime targetDate)
		{
			var features = new List<IFeature>();

			foreach (var sismo in events)
			{
				// Filter by selected date
				if (sismo.Time.Date != targetDate.Date) continue;

				// Skip invalid data
				if (sismo.MagnitudeValue == null || sismo.MagnitudeValue <= 0) continue;

				var point = SphericalMercator.FromLonLat(sismo.Lon, sismo.Lat).ToMPoint();
				var feature = new PointFeature(point);

				var location = !string.IsNullOrEmpty(sismo.Local) ? sismo.Local : sismo.ObservedRegion;
				var intensity = !string.IsNullOrEmpty(sismo.Degree) ? $" | Intensity: {sismo.Degree}" : "";
				var infoText = $"{location}\nMag: {sismo.Magnitude}{intensity}";

				var scale = Math.Max(0.8, sismo.MagnitudeValue.Value / 2.5);
				var color = GetColorByDegree(sismo.Degree);

				feature.Styles.Add(new SymbolStyle
				{
					SymbolScale = scale,
					SymbolType = SymbolType.Ellipse,
					Fill = new Brush(color),
					Outline = new Pen(Color.Black, 0.5)
				});

				feature.Styles.Add(new LabelStyle
				{
					Text = infoText,
					ForeColor = Color.Black,
					BackColor = new Brush(Color.FromArgb(180, 255, 255, 255)),
					Font = new Font { Size = 11 },
					HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Left,
					Offset = new Offset(15, 0),
					CollisionDetection = true
				});

				features.Add(feature);
			}

			var oldLayer = Map.Layers.FirstOrDefault(l => l.Name == "Sismos");
			if (oldLayer != null) Map.Layers.Remove(oldLayer);

			Map.Layers.Add(new MemoryLayer { Name = "Sismos", Features = features });
			Map.RefreshGraphics();
		}

		private Color GetColorByDegree(string? degree)
		{
			if (string.IsNullOrEmpty(degree))
				return Color.FromArgb(180, 255, 0, 0); // Default Red (Not felt)

			// Normaliza para facilitar (converte romanos para números se necessário)
			var d = degree.ToUpper().Trim();

			return d switch
			{
				"I" or "1" or "II" or "2" => Color.FromArgb(200, 50, 205, 50),   // LimeGreen
				"III" or "3" => Color.FromArgb(200, 255, 215, 0),  // Gold
				"IV" or "4" => Color.FromArgb(200, 255, 140, 0),  // DarkOrange
				"V" or "5" => Color.FromArgb(220, 255, 69, 0),   // OrangeRed
				"VI" or "6" or "VII" or "7" => Color.FromArgb(255, 139, 0, 0), // DarkRed
				_ => Color.FromArgb(255, 75, 0, 130)                           // Indigo (Very strong/Unknown)
			};
		}

		partial void OnSelectedDateChanged(DateTime value)
		{
			UpdateMapFilter();
		}

		private void UpdateMapFilter()
		{
			if (SeismicActivities == null) return;

			// Filters the list and updates the map
			AddSeismicLayerToMap(SeismicActivities, SelectedDate);
		}

		#region API
		private async Task LoadSeismologyDataAsync()
		{
			SeismicActivities.Clear();
			var response = await _apiClient.GetSeismicAsync(7);

			if (response?.Data != null)
			{
				var mapped = _mapper.Map<List<SeismicActivityDto>>(response.Data);
				SeismicActivities.AddRange(mapped);
			}
		}
		#endregion
	}
}
