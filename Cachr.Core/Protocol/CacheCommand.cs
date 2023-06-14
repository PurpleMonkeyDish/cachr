using System.Collections.Immutable;

namespace Cachr.Core.Protocol;

public sealed class CacheCommand
{
    public CacheCommandType CommandType { get; init; }
    public long CommandReferenceId { get; init; }
    public ImmutableArray<CacheValue> Arguments { get; init; }
}
