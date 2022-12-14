namespace Cachr.Core.Discovery;

public sealed class DefaultPeerDiscoveryProvider : IPeerDiscoveryProvider
{
    private Task<IEnumerable<string>> _task = Task.FromResult(Enumerable.Empty<string>());
    public Task<IEnumerable<string>> DiscoverPeersAsync(CancellationToken cancellationToken) => _task;
}
