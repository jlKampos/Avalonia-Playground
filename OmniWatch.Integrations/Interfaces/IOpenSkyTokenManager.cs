using OmniWatch.Integrations.Contracts.OpenSky;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniWatch.Integrations.Interfaces
{
    public interface IOpenSkyTokenManager
    {
        Task<string> GetRoleAsync();

        Task<string?> GetTokenAsync();
        Task<OpenSkyAuthResult> RefreshTokenAsync();
        Task<OpenSkyAuthResult> TestCredentialsAsync(string clientId, string clientSecret);

    }

}
