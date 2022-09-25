using System.Collections.Immutable;

namespace Cachr.Core.Peering;

public sealed record PeerStateInformation(Guid Id, ImmutableHashSet<Guid> ConnectedPeers,
    ImmutableHashSet<Guid> AvailablePeers)
{
    public DateTimeOffset LastSeen { get; init; } = DateTimeOffset.Now;
}
