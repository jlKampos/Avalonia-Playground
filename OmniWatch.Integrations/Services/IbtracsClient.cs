using OmniWatch.Integrations.Interfaces;

namespace OmniWatch.Integrations.Services
{
    public class IbtracsClient : IIbtracsClient
    {
        private readonly HttpClient _httpClient;
        private string? _resolvedUrl;

        private const string BaseIndexUrl = "https://www.ncei.noaa.gov/data/international-best-track-archive-for-climate-stewardship-ibtracs/";

        public IbtracsClient(HttpClient httpClient) => _httpClient = httpClient;

        private async Task<string> GetLatestVersionUrlAsync(CancellationToken ct)
        {
            var response = await _httpClient.GetStringAsync(BaseIndexUrl, ct);

            var matches = System.Text.RegularExpressions.Regex.Matches(response, @"v\d{2}r\d{2}/");

            var latestVersion = matches
                .Cast<System.Text.RegularExpressions.Match>()
                .Select(m => m.Value.TrimEnd('/'))
                .OrderByDescending(v => v)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(latestVersion))
                throw new Exception("Could not find a valid IBTrACS version folder.");

            return $"{BaseIndexUrl}{latestVersion}/access/csv/ibtracs.ALL.list.{latestVersion}.csv";
        }

        private async Task<string> ResolveUrlAsync(CancellationToken ct)
        {
            if (_resolvedUrl != null) return _resolvedUrl;
            _resolvedUrl = await GetLatestVersionUrlAsync(ct);
            return _resolvedUrl;
        }

        public async Task<(Stream Stream, DateTimeOffset LastModified)> GetRemoteStreamAsync(CancellationToken ct)
        {
            var url = await ResolveUrlAsync(ct);
            var localPath = Path.Combine(Path.GetTempPath(), "ibtracs_cache.csv");
            var remoteLastModified = await GetRemoteLastModifiedAsync(ct) ?? DateTimeOffset.UtcNow;

            bool exists = File.Exists(localPath);
            bool isStale = exists && remoteLastModified > File.GetLastWriteTimeUtc(localPath);

            if (!exists || isStale)
            {
                var tempDownloadPath = Path.Combine(Path.GetTempPath(), $"ibtracs_{Guid.NewGuid()}.tmp");

                try
                {
                    using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct))
                    {
                        response.EnsureSuccessStatusCode();
                        using (var remoteStream = await response.Content.ReadAsStreamAsync(ct))
                        using (var localFileStream = new FileStream(tempDownloadPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await remoteStream.CopyToAsync(localFileStream, ct);
                        }
                    }

                    if (File.Exists(localPath)) File.Delete(localPath);
                    File.Move(tempDownloadPath, localPath);
                }
                finally
                {
                    if (File.Exists(tempDownloadPath)) File.Delete(tempDownloadPath);
                }
            }

            var stream = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return (stream, remoteLastModified);
        }

        public async Task<DateTimeOffset?> GetRemoteLastModifiedAsync(CancellationToken ct)
        {
            try
            {
                var url = await ResolveUrlAsync(ct);
                using var request = new HttpRequestMessage(HttpMethod.Head, url);
                using var response = await _httpClient.SendAsync(request, ct);
                return response.Content.Headers.LastModified;
            }
            catch { return null; }
        }
    }
}
