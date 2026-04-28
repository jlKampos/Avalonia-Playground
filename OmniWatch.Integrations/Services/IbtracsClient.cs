using OmniWatch.Core.Interfaces;
using OmniWatch.Integrations.Interfaces;
using System.Text.Json;

namespace OmniWatch.Integrations.Services
{
    public class IbtracsClient : IIbtracsClient
    {
        private readonly IGlobalProgressService _progress;

        private readonly HttpClient _httpClient;

        private readonly string _csvPath;
        private readonly string _metaPath;

        private const string RemoteUrl =
            "https://www.ncei.noaa.gov/data/international-best-track-archive-for-climate-stewardship-ibtracs/" +
            "v04r01/access/csv/ibtracs.ALL.list.v04r01.csv";

        public IbtracsClient(HttpClient httpClient, IGlobalProgressService progress)
        {
            _httpClient = httpClient;
            _progress = progress;
            var baseFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "OmniWatch", "ibtracs");

            Directory.CreateDirectory(baseFolder);

            _csvPath = Path.Combine(baseFolder, "ibtracs.csv");
            _metaPath = Path.Combine(baseFolder, "metadata.json");
        }

        private void Report(string msg) => _progress.Report(msg);

        public async Task<string> GetLocalCsvPathAsync()
        {
            Report("Checking IBTrACS dataset…");
            if (!await IsUpToDateAsync())
                await DownloadAsync();

            return _csvPath;
        }

        private async Task<bool> IsUpToDateAsync()
        {
            if (!File.Exists(_csvPath) || !File.Exists(_metaPath))
            {
                Report("Ibtracs track missing");
                return false;
            }

            var head = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, RemoteUrl));
            head.EnsureSuccessStatusCode();

            var remoteLastModified = head.Content.Headers.LastModified;

            var meta = JsonSerializer.Deserialize<Metadata>(File.ReadAllText(_metaPath));

            return remoteLastModified <= meta.LastModified;
        }

        private async Task DownloadAsync()
        {
            Report("Downloading ibtracs dataset");
            var response = await _httpClient.GetAsync(RemoteUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var file = File.Create(_csvPath);
            await stream.CopyToAsync(file);

            var lastModified = response.Content.Headers.LastModified ?? DateTimeOffset.UtcNow;

            var meta = new Metadata { LastModified = lastModified };
            Report($"IBTrACS dataset downloaded (last modified: {lastModified})");
            File.WriteAllText(_metaPath, JsonSerializer.Serialize(meta));
        }

        private class Metadata
        {
            public DateTimeOffset LastModified { get; set; }

        }
    }
}
