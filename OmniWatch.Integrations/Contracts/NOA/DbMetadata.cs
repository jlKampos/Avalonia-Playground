using System.ComponentModel.DataAnnotations;

namespace OmniWatch.Integrations.Contracts.NOA
{
    public class DbMetadata
    {
        [Key]
        public string Key { get; set; } = string.Empty; // Ex: "Ibtracs_LastModified"
        public DateTimeOffset LastValue { get; set; }
    }
}
