namespace OmniWatch.Core.Interfaces
{
    public interface ISecretService
    {
        void Save(string value);
        string? Load();
    }
}
