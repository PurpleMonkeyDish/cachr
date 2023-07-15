using System.Runtime.CompilerServices;
using Cachr.Core.Data;

namespace Cachr.Core.Cache;

public enum UpdateType
{
    Local,
    Remote
}
public interface ICacheStorage
{
    Task<CacheEntry?> GetMetadataAsync(string key, int shard, CancellationToken cancellationToken);

    Task<CacheEntry?> UpdateExpiration(string key,
        int shard,
        DateTimeOffset? absoluteExpiration,
        TimeSpan? slidingExpiration,
        CancellationToken cancellationToken);

    Task Touch(string key, int shard, CancellationToken cancellationToken);

    Task SyncRecordAsync(
        CacheEntry remoteRecord,
        Func<CacheEntry, UpdateType, Task> needsUpdateCallback,
        CancellationToken cancellationToken);

    IAsyncEnumerable<CacheEntry> SampleShardAsync(
        int shard,
        double percentage = 0.01,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<CacheEntry> StreamShardAsync(int shard,
        [EnumeratorCancellation] CancellationToken cancellationToken);

    Task<int> ReapAsync(int count, CancellationToken cancellationToken);

    Task CreateOrReplaceEntryAsync(string key,
        int shard,
        DateTimeOffset? absoluteExpiration,
        TimeSpan? slidingExpiration,
        Func<Stream, Task> callbackAsync,
        CancellationToken cancellationToken);

    Task PurgeShard(int shard, CancellationToken cancellationToken);
}
