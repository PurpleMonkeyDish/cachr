using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cachr.Core;

public class ShardedMemoryCacheStorage : ICacheStorage, IDisposable
{
    private readonly int _indexMask;
    private IMemoryCache[]? _memoryCaches;
    internal IMemoryCache[]? MemoryCaches => _memoryCaches;

    public ShardedMemoryCacheStorage(IOptions<CachrDistributedCacheOptions> options, ILoggerFactory loggerFactory)
    {
        if (options.Value.Shards > 30 || options.Value.Shards < 0) throw new ArgumentException("Shard power must be between 0 and 30", nameof(options));
        if (options.Value.MaximumMemoryMegabytes < 1) throw new ArgumentException("Maximum memory must be at least 1MB", nameof(options));
        var shardCount = (int)Math.Pow(2, options.Value.Shards);
        _indexMask = shardCount - 1;
        _memoryCaches = new IMemoryCache[shardCount];
        var shardMaxMemorySizeMegabytes = Math.Max(1, options.Value.MaximumMemoryMegabytes / shardCount);
        var cacheOptions = new MemoryCacheOptions()
        {
            SizeLimit = shardMaxMemorySizeMegabytes * 1024L * 1024L,
            ExpirationScanFrequency = TimeSpan.FromSeconds(10) * shardCount 
        };
        for (var x = 0; x < _memoryCaches.Length; x++)
        {
            var memoryCache = new MemoryCache(cacheOptions, loggerFactory);
            _memoryCaches[x] = memoryCache;
        }
    }

    private IMemoryCache GetShard(string key)
    {
        if (_memoryCaches is null) throw new ObjectDisposedException(nameof(ShardedMemoryCacheStorage));
        var slot = key.GetHashCode() & _indexMask;
        return _memoryCaches[slot];
    }
    
    public void Set(string key, byte[] obj, TimeSpan? slidingExpiration = null, DateTimeOffset? absoluteExpiration = null)
    {
        using var cacheEntry = GetShard(key).CreateEntry(key);
        cacheEntry.Value = obj;
        cacheEntry.Size = obj.Length;
        cacheEntry.AbsoluteExpiration = absoluteExpiration;
        cacheEntry.SlidingExpiration = slidingExpiration;
        cacheEntry.PostEvictionCallbacks.Add(new PostEvictionCallbackRegistration()
        {
            EvictionCallback = OnCacheEntryEvicted,
        });
    }

    private void OnCacheEntryEvicted(object key, object value, EvictionReason reason, object state)
    {
        Debug.Assert(key is string);
        OnKeyEvicted((string)key, reason);
    }

    public bool TryGet(string key, out byte[] obj)
    {
        var cache = GetShard(key);
        return cache.TryGetValue(key, out obj);
    }

    public void Remove(string key)
    {
        var cache = GetShard(key);
        cache.Remove(key);
    }

    public event EventHandler<KeyEvictedEventArgs>? KeyEvicted;
    private void OnKeyEvicted(string key, EvictionReason evictionReason)
    {
        if (KeyEvicted is null) return;
        var eventArgs = new KeyEvictedEventArgs() {Key = key, EvictionReason = evictionReason};
        KeyEvicted?.Invoke(this, eventArgs);
    }

    public void Dispose()
    {
        var caches = Interlocked.Exchange(ref _memoryCaches, null);
        if (caches is null) return;
        foreach (var cache in caches)
        {
            cache.Dispose();
        }
    }
}