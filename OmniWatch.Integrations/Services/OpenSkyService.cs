using OmniWatch.Integrations.Contracts.OpenSky;
using OmniWatch.Integrations.Enums;
using OmniWatch.Integrations.Exceptions;
using OmniWatch.Integrations.Helpers;
using OmniWatch.Integrations.Interfaces;

namespace OmniWatch.Integrations.Services
{
    public class OpenSkyService : IOpenSkyService
    {
        private readonly IApiClient _api;

        public OpenSkyService(IApiClient api)
        {
            _api = api;
        }

        public async Task<OpenSkyResponse> GetAllFlightStatesAsync()
        {
            try
            {
                var raw = await _api.GetAsync<OpenSkyRawResponse>("states/all", ApiType.OpenSky);

                if (raw == null)
                    return new OpenSkyResponse
                    {
                        Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        States = new List<StateVectorItem>()
                    };

                return new OpenSkyResponse
                {
                    Time = raw.Time,
                    States = raw.States?
                        .Select(OpenSkyRawConverter.ConvertRaw)
                        .ToList() ?? new List<StateVectorItem>()
                };
            }
            catch (Exception ex)
            {
                throw new ApiException("Failed to load flight states data", ex);
            }

        }
    }
}

