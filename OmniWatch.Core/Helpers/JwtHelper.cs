using System.Text;
using System.Text.Json;

namespace OmniWatch.Core.Helpers
{
    public static class JwtHelper
    {
        private static string FixBase64(string input)
        {
            input = input.Replace('-', '+').Replace('_', '/');
            return input.PadRight(input.Length + (4 - input.Length % 4) % 4, '=');
        }

        public static JsonElement DecodePayload(string jwt)
        {
            var parts = jwt.Split('.');

            if (parts.Length != 3)
                throw new ArgumentException("Invalid JWT format");

            var payload = FixBase64(parts[1]);

            var bytes = Convert.FromBase64String(payload);
            var json = Encoding.UTF8.GetString(bytes);

            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }

        public static bool IsExpired(string jwt)
        {
            try
            {
                var parts = jwt.Split('.');

                if (parts.Length < 2)
                    return true;

                var payload = FixBase64(parts[1]);

                var jsonBytes = Convert.FromBase64String(payload);
                using var json = JsonDocument.Parse(jsonBytes);

                if (!json.RootElement.TryGetProperty("exp", out var exp))
                    return true;

                var expTime = DateTimeOffset.FromUnixTimeSeconds(exp.GetInt64());

                return expTime <= DateTimeOffset.UtcNow;
            }
            catch
            {
                return true;
            }
        }
    }
}
