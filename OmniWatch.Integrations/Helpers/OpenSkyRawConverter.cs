using OmniWatch.Integrations.Contracts.OpenSky;
using System.Text.Json;

namespace OmniWatch.Integrations.Helpers
{
    public class OpenSkyRawConverter
    {
        public static StateVectorItem ConvertRaw(List<JsonElement> raw)
        {
            return new StateVectorItem
            {
                Icao24 = raw[0].GetString(),
                Callsign = raw[1].GetString()?.Trim(),
                OriginCountry = raw[2].GetString(),

                TimePosition = TryGetLong(raw[3]),
                LastContact = TryGetLong(raw[4]),

                Longitude = TryGetDouble(raw[5]),
                Latitude = TryGetDouble(raw[6]),
                BaroAltitude = TryGetDouble(raw[7]),

                OnGround = TryGetBool(raw[8]),
                Velocity = TryGetDouble(raw[9]),
                TrueTrack = TryGetDouble(raw[10]),
                VerticalRate = TryGetDouble(raw[11]),

                Sensors = TryGetIntArray(raw[12]),

                GeoAltitude = TryGetDouble(raw[13]),
                Squawk = raw[14].GetString(),
                Spi = TryGetBool(raw[15]),
                PositionSource = TryGetInt(raw[16])
            };
        }

        private static double? TryGetDouble(JsonElement el)
        {
            if (el.ValueKind == JsonValueKind.Null) return null;
            if (el.ValueKind == JsonValueKind.Number && el.TryGetDouble(out var d)) return d;
            if (el.ValueKind == JsonValueKind.String && double.TryParse(el.GetString(), out var s)) return s;
            return null;
        }

        private static long? TryGetLong(JsonElement el)
        {
            if (el.ValueKind == JsonValueKind.Null) return null;
            if (el.ValueKind == JsonValueKind.Number && el.TryGetInt64(out var d)) return d;
            if (el.ValueKind == JsonValueKind.String && long.TryParse(el.GetString(), out var s)) return s;
            return null;
        }

        private static bool? TryGetBool(JsonElement el)
        {
            if (el.ValueKind == JsonValueKind.Null) return null;
            if (el.ValueKind == JsonValueKind.True) return true;
            if (el.ValueKind == JsonValueKind.False) return false;
            if (el.ValueKind == JsonValueKind.String && bool.TryParse(el.GetString(), out var b)) return b;
            return null;
        }

        private static int? TryGetInt(JsonElement el)
        {
            if (el.ValueKind == JsonValueKind.Null) return null;
            if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var d)) return d;
            if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out var s)) return s;
            return null;
        }

        private static int[]? TryGetIntArray(JsonElement el)
        {
            if (el.ValueKind != JsonValueKind.Array) return null;
            return el.EnumerateArray()
                     .Where(x => x.ValueKind == JsonValueKind.Number && x.TryGetInt32(out _))
                     .Select(x => x.GetInt32())
                     .ToArray();
        }

    }
}
