using System.Collections.Immutable;

namespace Cachr.Core.Discovery;

public record struct PeerStateUpdateMessage(Guid Id, PeerState State, Peer Peer, ImmutableArray<Peer> Connections)
{
    public static PeerStateUpdateMessage Create(PeerState state, Peer peer, IEnumerable<Peer> connections)
    {
        return new(
            peer.Id,
            state,
            peer,
            connections is ImmutableArray<Peer> array ? array : connections.ToImmutableArray()
        );
    }
}
