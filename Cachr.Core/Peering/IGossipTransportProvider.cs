using Cachr.Core.Discovery;

namespace Cachr.Core.Peering;

public interface IGossipTransportProvider
{
    IEnumerable<IGossipTransport> GetCurrentConnections();
    Task<IGossipTransport?> TryConnectToPeerAsync(Peer peer, CancellationToken cancellationToken);
}