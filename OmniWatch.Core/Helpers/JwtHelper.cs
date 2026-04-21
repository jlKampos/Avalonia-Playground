using System.Text;
using System.Text.Json;

namespace OmniWatch.Core.Helpers
{
    public static class JwtHelper
    {
        public static JsonElement DecodePayload(string jwt)
        {
            var parts = jwt.Split('.');

            if (parts.Length != 3)
                throw new ArgumentException("Invalid JWT format");

            var payload = parts[1];

            // Corrigir padding Base64
            payload = payload.Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

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

                var payload = parts[1];

                // fix base64 padding
                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

                var jsonBytes = Convert.FromBase64String(payload);
                var json = JsonDocument.Parse(jsonBytes);

                if (!json.RootElement.TryGetProperty("exp", out var exp))
                    return true;

                var expTime = DateTimeOffset.FromUnixTimeSeconds(exp.GetInt64());

                return expTime <= DateTimeOffset.UtcNow;
            }
            catch
            {
                return true; // assume invalid if parsing fails
            }
        }
    }

}
