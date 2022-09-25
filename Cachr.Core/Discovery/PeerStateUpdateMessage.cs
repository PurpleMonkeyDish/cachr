using System.Collections.Immutable;

namespace Cachr.Core.Discovery;

public sealed record PeerStateUpdateMessage(Guid Id, PeerState State, Peer Peer, ImmutableHashSet<Guid> ActiveConnections, ImmutableHashSet<Guid> AvailableConnections, long TimeStamp)
{
    public static PeerStateUpdateMessage Create(PeerState state, Peer peer, IEnumerable<Guid> activeConnections, IEnumerable<Guid> availableConnections)
    {
        return new(
            peer.Id,
            state,
            peer,
            activeConnections.ToImmutableHashSet(),
            availableConnections.ToImmutableHashSet(),
            TimeStamp: DateTimeOffset.Now.ToUnixTimeMilliseconds()
        );
    }
}
