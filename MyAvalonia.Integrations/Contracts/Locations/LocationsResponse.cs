using System.Text.Json.Serialization;

namespace MyAvalonia.Integrations.Contracts.Locations
{
	public class LocationsResponse
	{
		[JsonPropertyName("owner")]
		public string Owner { get; set; } = String.Empty;

		[JsonPropertyName("country")]
		public string Country { get; set; } = String.Empty;

		[JsonPropertyName("data")]
		public List<LocationItem> Data { get; set; } = new();
	}
}
