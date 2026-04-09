using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MyAvalonia.Integrations.Contracts.Awarness
{
	public class AwarenessItem
	{
		[JsonPropertyName("text")]
		public string Text { get; set; } = string.Empty;

		[JsonPropertyName("awarenessTypeName")]
		public string AwarenessTypeName { get; set; } = string.Empty;

		[JsonPropertyName("idAreaAviso")]
		public string IdAreaAviso { get; set; } = string.Empty;

		[JsonPropertyName("startTime")]
		public DateTime StartTime { get; set; }

		[JsonPropertyName("awarenessLevelID")]
		public string AwarenessLevelID { get; set; } = string.Empty;

		[JsonPropertyName("endTime")]
		public DateTime EndTime { get; set; }
	}
}
