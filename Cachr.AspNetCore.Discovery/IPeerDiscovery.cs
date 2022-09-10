namespace Cachr.AspNetCore.Discovery;

public interface IPeerDiscovery
{
    Task<IEnumerable<Peer>> GetPeersAsync(CancellationToken cancellationToken);
}