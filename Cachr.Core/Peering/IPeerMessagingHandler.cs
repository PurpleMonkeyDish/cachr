using Cachr.Core.Discovery;

namespace Cachr.Core.Peering;

public interface IPeerMessagingHandler
{
    ValueTask NotifyGossipReceivedAsync(CacheGossipMessage gossipMessage);
}
