namespace OmniWatch.Core.Settings
{
    public class AppSettings
    {
        public bool UseOpenSkyCredentials { get; set; } = false;
        public int RefreshInterval { get; set; } = 10;
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string Language { get; set; } = "en-US";
    }
}
