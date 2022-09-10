using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Cachr.Core;

public class CachrDistributedCache : IDistributedCache
{
    private readonly ICacheStorage _storage;
    private readonly ICacheBus _cacheBus;

    public CachrDistributedCache(ICacheStorage storage, ICacheBus cacheBus)
    {
        _storage = storage;
        _cacheBus = cacheBus;
        _storage.KeyEvicted += OnStorageKeyEvicted;
        _cacheBus.DataReceived += OnDataReceived;
    }

    private void OnDataReceived(object? sender, CacheBusDataReceivedEventArgs e)
    {
        
    }

    private void OnStorageKeyEvicted(object? sender, KeyEvictedEventArgs e)
    {
        
    }

    public async Task Preload(CancellationToken cancellationToken)
    {
    }
    public byte[]? Get(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _storage.Get(key);
    }

    public Task<byte[]?> GetAsync(string key, CancellationToken token = new CancellationToken())
    {
        ArgumentNullException.ThrowIfNull(key);
        return Task.FromResult(_storage.Get(key));
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions? options)
    {
        ArgumentNullException.ThrowIfNull(key);
        SetInternal(key, value, options);
    }

    private void SetInternal(string key, byte[] value, DistributedCacheEntryOptions? options)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        DateTimeOffset? absoluteExpiration = null;
        TimeSpan? slidingExpiration = null;
        if (options is not null)
        {
            absoluteExpiration = options.AbsoluteExpiration;
            slidingExpiration = options.SlidingExpiration;
            if (absoluteExpiration is null && options.AbsoluteExpirationRelativeToNow is not null)
                absoluteExpiration = DateTimeOffset.Now.Add(options.AbsoluteExpirationRelativeToNow.Value);
        }

        _storage.Set(key, value, slidingExpiration, absoluteExpiration);
        NotifyKeySet(key, value, slidingExpiration, absoluteExpiration);
    }

    private void NotifyKeySet(string key, byte[] value, TimeSpan? slidingExpiration, DateTimeOffset? absoluteExpiration)
    {
        
    }

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options,
        CancellationToken token = new CancellationToken())
    {
        SetInternal(key, value, options);
        return Task.CompletedTask;
    }

    public void Refresh(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
    }

    public Task RefreshAsync(string key, CancellationToken token = new CancellationToken())
    {
        ArgumentNullException.ThrowIfNull(key);
        return Task.CompletedTask;
    }

    public void Remove(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        _storage.Remove(key);
    }

    public Task RemoveAsync(string key, CancellationToken token = new CancellationToken())
    {
        ArgumentNullException.ThrowIfNull(key);
        _storage.Remove(key);
        return Task.CompletedTask;
    }
}