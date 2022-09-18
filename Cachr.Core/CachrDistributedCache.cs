using Cachr.Core.Messages;
using Cachr.Core.Messages.Encoder;
using Cachr.Core.Messaging;
using Cachr.Core.Storage;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Cachr.Core;

public sealed class CachrDistributedCache : ICachrDistributedCache, IDisposable
{
    private readonly ISubscriptionToken _subscriptionToken;
    private readonly IOptions<CachrDistributedCacheOptions> _options;
    private readonly ICacheStorage _storage;
    private readonly IMessageBus<OutboundCacheMessageEnvelope> _messageBus;

    public CachrDistributedCache
    (
        ICacheStorage storage,
        IOptions<CachrDistributedCacheOptions> options,
        IMessageBus<InboundCacheMessageEnvelope> inboundMessageBus,
        IMessageBus<OutboundCacheMessageEnvelope> outboundMessageBus
        )
    {
        _storage = storage;
        _options = options;
        _subscriptionToken = inboundMessageBus.Subscribe(OnCacheMessageReceived);
        _messageBus = outboundMessageBus;
    }

    private async Task OnCacheMessageReceived(InboundCacheMessageEnvelope envelope)
    {
        if (envelope.Target != null && envelope.Target != NodeIdentity.Id)
            return;
        HandleMessage(envelope.Message, envelope.Sender);
    }

    public void BeginPreload()
    {
    }

    public byte[]? Get(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _storage.Get(key);
    }

    public Task<byte[]?> GetAsync(string key, CancellationToken token = new())
    {
        ArgumentNullException.ThrowIfNull(key);
        return Task.FromResult(_storage.Get(key));
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions? options)
    {
        ArgumentNullException.ThrowIfNull(key);
        SetInternal(key, value, options);
    }

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options,
        CancellationToken token = new())
    {
        SetInternal(key, value, options);
        return Task.CompletedTask;
    }

    public void Refresh(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
    }

    public Task RefreshAsync(string key, CancellationToken token = new())
    {
        ArgumentNullException.ThrowIfNull(key);
        _storage.TryGet(key, out _);
        return Task.CompletedTask;
    }

    public void Remove(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        _storage.Remove(key);
        NotifyRemoved(key);
    }

    private async Task NotifyRemovedAsync(string key)
    {
        await BroadcastAsync(new KeyDeletedDistributedCacheMessage(key)).ConfigureAwait(false);
    }

    private void NotifyRemoved(string key)
    {
        NotifyRemovedAsync(key).ContinueWith(async t =>
        {
            try
            {
                await t.ConfigureAwait(false);
            }
            catch
            {
                // Ignored.
            }
        });
    }

    public async Task RemoveAsync(string key, CancellationToken token = new())
    {
        Remove(key);
        await NotifyRemovedAsync(key).ConfigureAwait(false);
    }

    private async Task HandleMessage(
        IDistributedCacheMessage message,
        Guid senderId
        )
    {
        switch (message.Type)
        {
            case DistributedCacheMessageType.GetKeys:
                await HandleTypedMessage((GetKeysDistributedCacheMessage)message, senderId).ConfigureAwait(false);
                break;
            case DistributedCacheMessageType.GetKeyData:
                await HandleTypedMessage((GetKeyDataDistributedCacheMessage)message, senderId).ConfigureAwait(false);
                break;
            case DistributedCacheMessageType.NoOperation:
                break;
            case DistributedCacheMessageType.GetKeyResponse:
                await HandleTypedMessage((GetKeysResponseDistributedCacheMessage)message).ConfigureAwait(false);
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
        {
            slidingExpiration = TimeSpan.FromMilliseconds(message.SlidingTimeToLiveMilliseconds);
        }

        if (message.ExpirationTimeStampUnixMilliseconds > 0)
        {
            absoluteExpiration = DateTimeOffset.FromUnixTimeMilliseconds(message.ExpirationTimeStampUnixMilliseconds);
            if (absoluteExpiration < DateTimeOffset.Now)
            {
                return;
            }
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

    private async Task HandleTypedMessage(GetKeysResponseDistributedCacheMessage message)
    {
        await SendToRandomAsync(new GetKeyDataDistributedCacheMessage(message.Key)).ConfigureAwait(false);
    }

    private async Task HandleTypedMessage(GetKeyDataDistributedCacheMessage message, Guid senderId)
    {
        if (!_storage.TryGet(message.Key, out var data))
        {
            return;
        }

        var reply = new GetKeyDataResponseDistributedCacheMessage(message.Key, data);
        await SendToAsync(senderId, reply).ConfigureAwait(false);
    }

    private async Task HandleTypedMessage(
        GetKeysDistributedCacheMessage message,
        Guid senderId
        )
    {
        foreach (var key in _storage.Keys)
        {
            await SendToAsync(senderId, new GetKeysResponseDistributedCacheMessage(key)).ConfigureAwait(false);
        }
    }

    private async Task SendToRandomAsync(IDistributedCacheMessage message)
    {

        await _messageBus.SendToRandomAsync(new OutboundCacheMessageEnvelope(null, message)).ConfigureAwait(false);
    }

    private async Task BroadcastAsync(IDistributedCacheMessage message)
    {
        await _messageBus.BroadcastAsync(new OutboundCacheMessageEnvelope(null, message)).ConfigureAwait(false);
    }

    private async Task SendToAsync(Guid targetId, IDistributedCacheMessage message)
    {
        await _messageBus.BroadcastAsync(new OutboundCacheMessageEnvelope(targetId, message)).ConfigureAwait(false);
    }

    private async Task SetInternalAsync(string key, byte[] value, DistributedCacheEntryOptions? options)
    {
        var (slidingExpiration, absoluteExpiration) = SetInternalCommon(key, value, options);
        await NotifyKeySetAsync(key, value, slidingExpiration, absoluteExpiration).ConfigureAwait(false);
    }

    private void SetInternal(string key, byte[] value, DistributedCacheEntryOptions? options)
    {
        var (slidingExpiration, absoluteExpiration) = SetInternalCommon(key, value, options);
        NotifyKeySet(key, value, slidingExpiration, absoluteExpiration);
    }

    private (TimeSpan? slidingExpiration, DateTimeOffset? absoluteExpiration) SetInternalCommon(string key, byte[] value, DistributedCacheEntryOptions? options)
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
            {
                absoluteExpiration = DateTimeOffset.Now.Add(options.AbsoluteExpirationRelativeToNow.Value);
            }
        }

        _storage.Set(key, value, slidingExpiration, absoluteExpiration);
        return (slidingExpiration, absoluteExpiration);
    }

    private void NotifyKeySet(string key, byte[] value, TimeSpan? slidingExpiration, DateTimeOffset? absoluteExpiration)
    {
        // Fling this off into oblivion
        // There's no follow up, and the wait is a lock wait only.
        NotifyKeySetAsync(key, value, slidingExpiration, absoluteExpiration)
            .ContinueWith(async t =>
            {
                try
                {
                    await t.ConfigureAwait(false);
                }
                catch
                {
                    // Ignored.
                }
            }).Unwrap();
    }

    private async Task NotifyKeySetAsync(string key, byte[] value, TimeSpan? slidingExpiration, DateTimeOffset? absoluteExpiration)
    {
        var slidingExpirationMilliseconds = (int)(slidingExpiration?.TotalMilliseconds ?? 0);
        var absoluteExpirationTimestampMilliseconds = absoluteExpiration?.ToUnixTimeMilliseconds() ?? 0;
        var message = new KeySetDistributedCacheMessage(key, value, slidingExpirationMilliseconds,
            absoluteExpirationTimestampMilliseconds);
        await BroadcastAsync(message).ConfigureAwait(false);
    }

    public void Dispose()
    {
        _subscriptionToken.Dispose();
    }
}
