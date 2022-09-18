namespace Cachr.Core.Discovery;

public interface IPeerDiscoveryProvider
{
    Task<IEnumerable<string>> DiscoverPeersAsync(CancellationToken cancellationToken);
}
