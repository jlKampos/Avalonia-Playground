using OmniWatch.Integrations.Contracts.NOA;
using OmniWatch.Integrations.Enums;
using OmniWatch.Integrations.Interfaces;
using System.Net.Http.Json;

namespace OmniWatch.Integrations.Services
{
    public class NoaaService : INoaaService
    {
        private readonly HttpClient _client;

        public NoaaService(IHttpClientFactory factory)
        {
            _client = factory.CreateClient(ApiType.Noaa.ToString());
        }

        public async Task<List<CycloneItem>> GetActiveCyclonesAsync()
        {
            var response =
                await _client.GetFromJsonAsync<NoaaStormResponse>("CurrentStorms.json");

            if (response?.Storms == null)
                return new List<CycloneItem>();

            return response.Storms.Select(Map).ToList();
        }

        public Task<CycloneItem?> GetCycloneByIdAsync(string id)
        {
            return Task.FromResult<CycloneItem?>(null);
        }

        private CycloneItem Map(NoaaStormItem storm)
        {
            return new CycloneItem
            {
                Id = storm.Id,
                Name = storm.Name,
                Latitude = storm.Latitude,
                Longitude = storm.Longitude,
                Category = storm.Category,
                WindSpeed = storm.Wind
            };
        }
    }
}
