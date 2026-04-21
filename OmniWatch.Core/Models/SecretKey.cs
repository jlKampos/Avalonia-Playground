using OmniWatch.Core.Enums;

namespace OmniWatch.Core.Models
{
    public record SecretKey(SecretType Type, string Name, string? Scope = null)
    {
        public string Name { get; } = Sanitize(Name.Trim().ToLowerInvariant());
        public string? Scope { get; } = Scope is null
            ? null
            : Sanitize(Scope.Trim().ToLowerInvariant());

        public string ToStorageKey()
            => Scope is null
                ? $"{(int)Type}:{Name}"
                : $"{(int)Type}:{Scope}:{Name}";

        private static string Sanitize(string value) => value.Replace(":", "_");
    }
}
