using Cachr.Core.Discovery;

namespace Cachr.Core.Peering;

public sealed class NoOpGossipTransportProvider : IGossipTransportProvider
{
    private readonly Task<IGossipTransport?> _completedTask = Task.FromResult<IGossipTransport?>(null);
    public IEnumerable<IGossipTransport> GetCurrentConnections()
    {
        return Enumerable.Empty<IGossipTransport>();
    }

    public Task<IGossipTransport?> TryConnectToPeerAsync(Peer peer, CancellationToken cancellationToken)
    {
        return _completedTask;
    }
}