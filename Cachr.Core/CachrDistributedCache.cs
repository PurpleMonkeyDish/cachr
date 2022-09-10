using System.Runtime.CompilerServices;
using System.Text;
using Cachr.Core.Messages;
using Cachr.Core.Messages.Bus;
using Cachr.Core.Messages.Encoder;
using Cachr.Core.Storage;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Cachr.Core;

public class CachrDistributedCache : ICachrDistributedCache
{
    private readonly ICacheStorage _storage;
    private readonly ICacheBus _cacheBus;
    private readonly IOptions<CachrDistributedCacheOptions> _options;

    public CachrDistributedCache(
        ICacheStorage storage, 
        ICacheBus cacheBus, 
        IOptions<CachrDistributedCacheOptions> options
        )
    {
        _storage = storage;
        _cacheBus = cacheBus;
        _options = options;
        _cacheBus.DataReceived += OnDataReceived;
    }

    private void OnDataReceived(object? sender, CacheBusDataReceivedEventArgs e)
    {
        using (e)
        {
            var message = e.Decode();
            HandleMessage(message, e);
        }
    }

    private void HandleMessage(IDistributedCacheMessage message, CacheBusDataReceivedEventArgs cacheBusDataReceivedEventArgs)
    {
        switch (message.Type)
        {
            case DistributedCacheMessageType.GetKeys:
                HandleTypedMessage((GetKeysDistributedCacheMessage)message, cacheBusDataReceivedEventArgs);
                break;

            case DistributedCacheMessageType.GetKeyData:
                HandleTypedMessage((GetKeyDataDistributedCacheMessage)message, cacheBusDataReceivedEventArgs);
                break;
            case DistributedCacheMessageType.NoOperation:
                break;
            case DistributedCacheMessageType.GetKeyResponse:
                HandleTypedMessage((GetKeysResponseDistributedCacheMessage)message);
                break;
            case DistributedCacheMessageType.GetKeyDataResponse:
                HandleTypedMessage((GetKeyDataResponseDistributedCacheMessage)message);
                break;
            case DistributedCacheMessageType.KeySet:
                HandleTypedMessage((KeySetDistributedCacheMessage)message);
                break;
            case DistributedCacheMessageType.KeyDelete:
                HandleTypedMessage((KeyDeletedDistributedCacheMessage)message);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void HandleTypedMessage(KeySetDistributedCacheMessage message)
    {
        TimeSpan? slidingExpiration = null;
        DateTimeOffset? absoluteExpiration = null;
        if (message.SlidingTimeToLiveMilliseconds > 0)
            slidingExpiration = TimeSpan.FromMilliseconds(message.SlidingTimeToLiveMilliseconds);

        if (message.ExpirationTimeStampUnixMilliseconds > 0)
        {
            absoluteExpiration = DateTimeOffset.FromUnixTimeMilliseconds(message.ExpirationTimeStampUnixMilliseconds);
            if (absoluteExpiration < DateTimeOffset.Now) return;
        }
        
        _storage.Set(message.Key, message.Data, slidingExpiration, absoluteExpiration);
    }

    private void HandleTypedMessage(KeyDeletedDistributedCacheMessage message)
    {
        _storage.Remove(message.Key);
    }

    private TimeSpan GetAbsoluteTimeToLiveForPreload()
    {
        var maximumTimeToLive = _options.Value.ColdStartTtlMaxSeconds;
        return TimeSpan.FromSeconds(Random.Shared.Next(maximumTimeToLive / 8, maximumTimeToLive));
    }

    private void HandleTypedMessage(GetKeyDataResponseDistributedCacheMessage message)
    {
        _storage.Set(message.Key, message.Data, null, DateTimeOffset.Now.Add(GetAbsoluteTimeToLiveForPreload()));
    }

    private void HandleTypedMessage(GetKeysResponseDistributedCacheMessage message)
    {
        var payload = DistributedCacheMessageEncoder.Encode(
            new GetKeyDataDistributedCacheMessage(message.Key)
        );
        _cacheBus.SendToOneRandom(payload);
    }

    private void HandleTypedMessage(GetKeyDataDistributedCacheMessage message, CacheBusDataReceivedEventArgs cacheBusDataReceivedEventArgs)
    {
        if (!_storage.TryGet(message.Key, out var data)) return;
        
        var reply = DistributedCacheMessageEncoder.Encode(new GetKeyDataResponseDistributedCacheMessage(message.Key, data));
        cacheBusDataReceivedEventArgs.Reply(reply);
    }

    private void HandleTypedMessage(GetKeysDistributedCacheMessage message, CacheBusDataReceivedEventArgs cacheBusDataReceivedEventArgs)
    {
        foreach (var key in _storage.Keys)
        {
            cacheBusDataReceivedEventArgs.Reply(DistributedCacheMessageEncoder.Encode(new GetKeysResponseDistributedCacheMessage(key)));
        }
    }

    public void BeginPreload()
    {
        _cacheBus.SendToOneRandom(DistributedCacheMessageEncoder.Encode(GetKeysDistributedCacheMessage.Instance));
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
        var slidingExpirationMilliseconds = (int) (slidingExpiration?.TotalMilliseconds ?? 0);
        var absoluteExpirationTimestampMilliseconds = (long) (absoluteExpiration?.ToUnixTimeMilliseconds() ?? 0);
        var message = new KeySetDistributedCacheMessage(key, value, slidingExpirationMilliseconds, absoluteExpirationTimestampMilliseconds);
        var encodedMessage = DistributedCacheMessageEncoder.Encode(message);
        _cacheBus.Broadcast(encodedMessage);
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
        var message = new KeyDeletedDistributedCacheMessage(key);
        _cacheBus.Broadcast(DistributedCacheMessageEncoder.Encode(message));
    }

    public Task RemoveAsync(string key, CancellationToken token = new CancellationToken())
    {
        Remove(key);
        return Task.CompletedTask;
    }
}