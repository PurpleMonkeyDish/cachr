using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cachr.Core.Storage;

public sealed class ShardedMemoryCacheStorage : ICacheStorage, IDisposable
{
    private readonly int _indexMask;
    private readonly HashSet<string>[]? _keys;
    private IMemoryCache[]? _memoryCaches;

    public ShardedMemoryCacheStorage(IOptions<CachrDistributedCacheOptions> options, ILoggerFactory loggerFactory)
    {
        if (options.Value.Shards > 30 || options.Value.Shards < 0)
        {
            throw new ArgumentException("Shard power must be between 0 and 30", nameof(options));
        }

        if (options.Value.MaximumMemoryMegabytes < 1)
        {
            throw new ArgumentException("Maximum memory must be at least 1MB", nameof(options));
        }

        var shardCount = (int)Math.Pow(2, options.Value.Shards);
        _indexMask = shardCount - 1;
        _memoryCaches = new IMemoryCache[shardCount];
        _keys = new HashSet<string>[shardCount];
        var shardMaxMemorySizeMegabytes = Math.Max(1, options.Value.MaximumMemoryMegabytes / shardCount);
        var cacheOptions = new MemoryCacheOptions
        {
            SizeLimit = shardMaxMemorySizeMegabytes * 1024L * 1024L,
            ExpirationScanFrequency = TimeSpan.FromSeconds(10) * shardCount
        };
        for (var x = 0; x < _memoryCaches.Length; x++)
        {
            var memoryCache = new MemoryCache(cacheOptions, loggerFactory);
            _memoryCaches[x] = memoryCache;
            _keys[x] = new HashSet<string>();
        }
    }

    internal IMemoryCache[]? MemoryCaches => _memoryCaches;

    public IEnumerable<KeyValuePair<string, byte[]>> AllEntries => GetAllEntries();

    public IEnumerable<string> Keys => EnumerateKeys().ToArray();

    public void Set(string key, byte[] obj, TimeSpan? slidingExpiration = null,
        DateTimeOffset? absoluteExpiration = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(obj);
        var slot = GetShard(key);
        using var cacheEntry = GetShardCache(slot).CreateEntry(key);
        cacheEntry.Value = obj;
        cacheEntry.Size = obj.Length;
        cacheEntry.AbsoluteExpiration = absoluteExpiration;
        cacheEntry.SlidingExpiration = slidingExpiration;
        var keys = GetShardKeys(slot);
        lock (keys)
        {
            keys.Add(key);
        }
    }

    public byte[]? Get(string key)
    {
        var shard = GetShard(key);
        if (!GetShardCache(shard).TryGetValue(key, out byte[] data))
        {
            return null;
        }

        return data;
    }

    public bool TryGet(string key, out byte[] obj)
    {
        ArgumentNullException.ThrowIfNull(key);

        var shard = GetShard(key);
        var cache = GetShardCache(shard);
        return cache.TryGetValue(key, out obj);
    }

    public void Remove(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        var shard = GetShard(key);
        var cache = GetShardCache(shard);
        var keys = GetShardKeys(shard);
        cache.Remove(key);
        lock (keys)
        {
            keys.Remove(key);
        }
    }

    public void Dispose()
    {
        var caches = Interlocked.Exchange(ref _memoryCaches, null);
        if (caches is null)
        {
            return;
        }

        foreach (var cache in caches)
        {
            cache.Dispose();
        }
    }

    private IEnumerable<string> EnumerateKeys()
    {
        ThrowIfDisposed();
        var keys = _keys;
        foreach (var set in keys!)
        {
            string[] setItems;
            lock (set)
            {
                setItems = set.ToArray();
            }

            foreach (var setItem in setItems)
            {
                yield return setItem;
            }

            ThrowIfDisposed();
        }
    }

    private IEnumerable<KeyValuePair<string, byte[]>> GetAllEntries()
    {
        ThrowIfDisposed();
        foreach (var key in EnumerateKeys())
        {
            ThrowIfDisposed();
            if (!TryGet(key, out var data))
            {
                continue;
            }

            yield return new KeyValuePair<string, byte[]>(key, data);
        }
    }

    private HashSet<string> GetShardKeys(int slot)
    {
        ThrowIfDisposed();
        return _keys![slot];
    }

    private int GetShard(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return key.GetHashCode() & _indexMask;
    }

    private void ThrowIfDisposed()
    {
        if (_memoryCaches is null || _keys is null)
        {
            throw new ObjectDisposedException(nameof(ShardedMemoryCacheStorage));
        }
    }

    private IMemoryCache GetShardCache(int slot)
    {
        ThrowIfDisposed();
        return _memoryCaches![slot];
    }
}
