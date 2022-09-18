using System.Collections.Immutable;

namespace Cachr.Core.Discovery;

public sealed record PeerStateUpdateMessage(Guid Id, PeerState State, Peer Peer, ImmutableArray<Peer> Connections, long TimeStamp)
{
    public static PeerStateUpdateMessage Create(PeerState state, Peer peer, IEnumerable<Peer> connections)
    {
        return new(
            peer.Id,
            state,
            peer,
            connections is ImmutableArray<Peer> array ? array : connections.ToImmutableArray(),
            TimeStamp: DateTimeOffset.Now.ToUnixTimeMilliseconds()
        );
    }
}
