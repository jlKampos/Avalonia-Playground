using OmniWatch.Integrations.Contracts.OpenSky;
using System.Text.Json;

namespace OmniWatch.Integrations.Helpers
{
    public static class OpenSkyRawConverter
    {
        public static StateVectorItem ConvertRaw(List<JsonElement> raw)
        {
            return new StateVectorItem
            {
                Icao24 = SafeGetString(raw, 0),
                Callsign = SafeGetString(raw, 1)?.Trim(),
                OriginCountry = SafeGetString(raw, 2),

                TimePosition = SafeGetLong(raw, 3),
                LastContact = SafeGetLong(raw, 4),

                Longitude = SafeGetDouble(raw, 5),
                Latitude = SafeGetDouble(raw, 6),
                BaroAltitude = SafeGetDouble(raw, 7),

                OnGround = SafeGetBool(raw, 8),
                Velocity = SafeGetDouble(raw, 9),
                TrueTrack = SafeGetDouble(raw, 10),
                VerticalRate = SafeGetDouble(raw, 11),

                Sensors = SafeGetIntArray(raw, 12),

                GeoAltitude = SafeGetDouble(raw, 13),

                Squawk = SafeGetString(raw, 14),
                Spi = SafeGetBool(raw, 15),
                PositionSource = SafeGetInt(raw, 16)
            };
        }

        // =========================
        // SAFE INDEX ACCESS
        // =========================
        private static JsonElement? SafeGet(List<JsonElement> raw, int index)
        {
            return index >= 0 && index < raw.Count
                ? raw[index]
                : null;
        }

        // =========================
        // STRING
        // =========================
        private static string? SafeGetString(List<JsonElement> raw, int index)
        {
            var el = SafeGet(raw, index);
            if (el is null) return null;

            return el.Value.ValueKind == JsonValueKind.String
                ? el.Value.GetString()
                : null;
        }

        // =========================
        // DOUBLE
        // =========================
        private static double? SafeGetDouble(List<JsonElement> raw, int index)
        {
            var el = SafeGet(raw, index);
            if (el is null) return null;

            var value = el.Value;

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var d))
                return d;

            if (value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(), out var s))
                return s;

            return null;
        }

        // =========================
        // LONG
        // =========================
        private static long? SafeGetLong(List<JsonElement> raw, int index)
        {
            var el = SafeGet(raw, index);
            if (el is null) return null;

            var value = el.Value;

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var l))
                return l;

            if (value.ValueKind == JsonValueKind.String && long.TryParse(value.GetString(), out var s))
                return s;

            return null;
        }

        // =========================
        // BOOL
        // =========================
        private static bool? SafeGetBool(List<JsonElement> raw, int index)
        {
            var el = SafeGet(raw, index);
            if (el is null) return null;

            var value = el.Value;

            return value.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String when bool.TryParse(value.GetString(), out var b) => b,
                _ => null
            };
        }

        // =========================
        // INT
        // =========================
        private static int? SafeGetInt(List<JsonElement> raw, int index)
        {
            var el = SafeGet(raw, index);
            if (el is null) return null;

            var value = el.Value;

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var i))
                return i;

            if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var s))
                return s;

            return null;
        }

        // =========================
        // INT ARRAY
        // =========================
        private static int[]? SafeGetIntArray(List<JsonElement> raw, int index)
        {
            var el = SafeGet(raw, index);
            if (el is null) return null;

            var value = el.Value;

            if (value.ValueKind != JsonValueKind.Array)
                return null;

            return value.EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.Number && x.TryGetInt32(out _))
                .Select(x => x.GetInt32())
                .ToArray();
        }
    }
}