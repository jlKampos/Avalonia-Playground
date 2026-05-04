using OmniWatch.Core.Interfaces;
using OmniWatch.Integrations.Interfaces;

namespace OmniWatch.Integrations.Services
{
    public class IbtracsClient : IIbtracsClient
    {
        private readonly HttpClient _httpClient;

        private const string RemoteUrl =
            "https://www.ncei.noaa.gov/data/international-best-track-archive-for-climate-stewardship-ibtracs/" +
            "v04r01/access/csv/ibtracs.ALL.list.v04r01.csv";

        public IbtracsClient(HttpClient httpClient) => _httpClient = httpClient;

        public async Task<(Stream Stream, DateTimeOffset LastModified)> GetRemoteStreamAsync(CancellationToken ct)
        {
            var response = await _httpClient.GetAsync(RemoteUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync(ct);
            var lastModified = response.Content.Headers.LastModified ?? DateTimeOffset.UtcNow;

            return (stream, lastModified);
        }

        public async Task<DateTimeOffset?> GetRemoteLastModifiedAsync(CancellationToken ct)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Head, RemoteUrl);
                using var response = await _httpClient.SendAsync(request, ct);
                return response.Content.Headers.LastModified;
            }
            catch { return null; }
        }
    }
}
