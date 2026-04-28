using OmniWatch.Core.Interfaces;

namespace OmniWatch.Core.Services
{
    public class GlobalProgressService : IGlobalProgressService
    {
        public event Action<string>? ProgressChanged;

        public void Report(string message)
        {
            ProgressChanged?.Invoke(message);
        }
    }
}
