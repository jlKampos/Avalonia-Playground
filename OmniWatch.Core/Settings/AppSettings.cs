namespace OmniWatch.Core.Settings
{
    public class AppSettings
    {
        public bool UseOpenSkyCredentials { get; set; } = false;

        public string? OpenSkyClientId { get; set; }

        public int RefreshInterval { get; set; } = 10;

        public string Language { get; set; } = "en-US";
    }

}
