using Cachr.Core.Discovery;

namespace Cachr.Core.Peering;

public interface IGossipTransport
{
    Peer Peer { get; }
    Task SendGossipAsync(CacheGossipMessage cacheGossipMessage, CancellationToken cancellationToken);
    Task DisconnectAsync(CancellationToken cancellationToken);
}