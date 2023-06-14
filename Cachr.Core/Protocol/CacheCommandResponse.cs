namespace Cachr.Core.Protocol;

public sealed class CacheCommandResponse
{
    public CacheCommandStatus Status { get; init; }
    public long CommandReferenceId { get; init; }
    public int Order { get; init; }
    public CacheValue Value { get; init; } = NullCacheValue.Instance;
}