namespace Cachr.Core.Data;

public record CacheEntry
{
    public required string Key { get; init; }
    public Guid MetadataId { get; init; }
    public int Shard { get; init; }
    public DateTimeOffset Created { get; init; }
    public DateTimeOffset Modified { get; init; }
    public DateTimeOffset? AbsoluteExpiration { get; init; }
    public TimeSpan? SlidingExpiration { get; init; }
    public DateTimeOffset LastAccess { get; init; }
}
