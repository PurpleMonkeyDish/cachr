using Microsoft.Extensions.Options;

namespace Cachr.Core;

public class CachrDistributedCacheOptions : IOptions<CachrDistributedCacheOptions>
{
    /// <summary>
    /// The number of cache shards to use. Shard count is 1 to the power of Shards - 1
    /// </summary>
    public int Shards { get; set; } = 2;
    /// <summary>
    /// Maximum total memory cache is allowed to consume
    /// </summary>
    public int MaximumMemoryMegabytes { get; set; } = 512;
    public CachrDistributedCacheOptions Value => this;
}