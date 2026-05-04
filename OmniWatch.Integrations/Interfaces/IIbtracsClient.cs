namespace OmniWatch.Integrations.Interfaces
{
    public interface IIbtracsClient
    {
        Task<(Stream Stream, DateTimeOffset LastModified)> GetRemoteStreamAsync(CancellationToken ct);
        Task<DateTimeOffset?> GetRemoteLastModifiedAsync(CancellationToken ct);
    }
}
