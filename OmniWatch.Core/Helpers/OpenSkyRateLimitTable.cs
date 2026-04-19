using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmniWatch.Core.Helpers
{
    public static class OpenSkyRateLimitTable
    {
        public static int GetDailyLimit(string role)
        {
            return role switch
            {
                "OPENSKY_API_DEFAULT" => 4000,
                "OPENSKY_API_FEEDER" => 8000,
                "OPENSKY_API_LICENSED" => 14400,
                _ => 4000 // fallback seguro
            };
        }

        public static DateTime GetDailyResetUtc()
        {
            return DateTime.UtcNow.Date.AddDays(1);
        }

    }
}
