namespace Cachr.Core;

public class CachrDistributedCacheOptions
{
    /// <summary>
    /// The number of cache shards to use. Shard count is 1 to the power of Shards - 1
    /// </summary>
    public int Shards { get; set; } = 2;
}