using System.Collections.Immutable;

namespace Cachr.Core.Discovery;

public sealed record Peer(Guid Id, ImmutableArray<string> EndPoints, string ProtocolPartition)
{
    public static Peer CreateSelf(IEnumerable<string> endPoints, string protocolPartition = "")
    {
        return new(
            NodeIdentity.Id,
            endPoints is ImmutableArray<string> array ? array : endPoints.ToImmutableArray(),
            protocolPartition
        );
    }
}
