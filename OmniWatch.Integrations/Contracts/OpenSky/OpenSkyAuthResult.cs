using OmniWatch.Integrations.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniWatch.Integrations.Contracts.OpenSky
{
    public class OpenSkyAuthResult
    {
        public OpenSkyAuthStatus Status { get; set; }
        public string? AccessToken { get; set; }
    }
}
