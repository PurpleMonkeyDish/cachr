using System.Collections.Immutable;

namespace Cachr.Core.Discovery;

public sealed record Peer(Guid Id, ImmutableArray<string> EndPoints, string ProtocolPartition)
{
    public static Peer Create(IEnumerable<string> endPoints, string protocolPartition = "")
    {
        return new(
            Guid.NewGuid(),
            endPoints is ImmutableArray<string> array ? array : endPoints.ToImmutableArray(),
            protocolPartition
        );
    }
}
