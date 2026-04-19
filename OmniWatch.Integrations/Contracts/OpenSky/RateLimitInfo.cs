using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniWatch.Integrations.Contracts.OpenSky
{
    public class RateLimitInfo
    {
        public int Limit { get; set; }
        public int Remaining { get; set; }
        public DateTime ResetAt { get; set; }
    }
}
