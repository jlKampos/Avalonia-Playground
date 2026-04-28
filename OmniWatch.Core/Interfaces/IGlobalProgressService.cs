using System;

namespace OmniWatch.Core.Interfaces
{
    public interface IGlobalProgressService
    {
        void Report(string message);
        event Action<string> ProgressChanged;
    }
}
