using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MyAvalonia.Models.Seismic
{
	public class SeismicActivityDto
	{
		public string? GoogleMapRef { get; set; }
		public string? Degree { get; set; }
		public string? SismoId { get; set; }
		public string? MagnitudeType { get; set; }
		public string? ObservedRegion { get; set; }
		public string? Source { get; set; }
		public string? TensorRef { get; set; }
		public string? Sensed { get; set; }
		public string? ShakemapId { get; set; }
		public string? ShakemapRef { get; set; }
		public string? Local { get; set; }

		public DateTime DataUpdate { get; set; }
		public DateTime Time { get; set; }
		public int Depth { get; set; }

		public string Longitude { get; set; } = "0";
		public string Latitude { get; set; } = "0";
		public string Magnitude { get; set; } = "0";

		// Helpers para lógica de negócio e Mapsui
		// Ignora o valor -99.0 que o IPMA usa para "sem dados"
		public double? MagnitudeValue =>
		double.TryParse(Magnitude, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v) && v != -99.0
		? v : null;

		public double Lon =>
			double.TryParse(Longitude, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v)
			? v : 0;

		public double Lat =>
			double.TryParse(Latitude, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v)
			? v : 0;
	}
}
