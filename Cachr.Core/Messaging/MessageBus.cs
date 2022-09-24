using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace Cachr.Core.Messaging;

public sealed class MessageBus<T> : IMessageBus<T>, IDisposable
{
    private readonly Channel<T> _broadcastMessages;
    private readonly Task _broadcastTask;


    private readonly Channel<T> _randomTargetChannel;

    private readonly ConcurrentDictionary<Guid, WeakReference> _subscriptions =
        new(
            Environment.ProcessorCount * 16,
            0
        );

    private IEnumerable<SubscriptionToken<T>>? _subscriptionCache;
    private IEnumerable<WeakReference>? _weakReferenceCache;

    public MessageBus(IOptions<MessageBusOptions> options)
    {
        _weakReferenceCache = null;
        _subscriptionCache = null;
        _broadcastMessages = Channel.CreateBounded<T>(options.Value.CreateChannelOptions());
        _randomTargetChannel = Channel.CreateBounded<T>(options.Value.CreateChannelOptions());
        _broadcastTask = Task.WhenAll(BroadcastProcessor(), RandomTargetMessageProcessor());
    }

    public async Task ShutdownAsync()
    {
        Dispose();
        await _broadcastTask.ConfigureAwait(false);
    }

    public void Dispose()
    {
        // Mark the channels complete to shutdown the processors.
        _broadcastMessages.Writer.TryComplete();
        _randomTargetChannel.Writer.TryComplete();
    }

    public async Task BroadcastAsync(T message, CancellationToken cancellationToken)
    {
        await _broadcastMessages.Writer.WriteAsync(
                message,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task SendToRandomAsync(T message, CancellationToken cancellationToken)
    {
        await _randomTargetChannel.Writer.WriteAsync(
            message,
            cancellationToken
        ).ConfigureAwait(false);
    }

    public ISubscriptionToken Subscribe(Func<T, ValueTask> callback, SubscriptionMode mode = SubscriptionMode.All)
    {
        return AddSubscription(new SubscriptionToken<T>(callback, this, mode: mode));
    }

    public ISubscriptionToken Subscribe(Func<T, object?, ValueTask> callback, object? state = null, SubscriptionMode mode = SubscriptionMode.All)
    {
        return AddSubscription(new SubscriptionToken<T>(callback, this, state, mode: mode));
    }

    public void Unsubscribe(ISubscriptionToken subscriptionToken)
    {
        ArgumentNullException.ThrowIfNull(subscriptionToken);
        if (!_subscriptions.TryRemove(subscriptionToken.Id, out _))
        {
            return;
        }

        // This will only recurse once, and only if we weren't called via the token.
        subscriptionToken.Dispose();
    }

    private async Task BroadcastProcessor()
    {
        static ValueTask<bool> Callback(SubscriptionToken<T> token, T message) => token.TryInvokeListener(message, SubscriptionMode.Broadcast);
        var broadcastCallback = Callback;
        await Task.Yield();
        try
        {
            await foreach (var message in _broadcastMessages.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                await ForEachSubscriberAsync(broadcastCallback, message);
                await CompleteMessage(message).ConfigureAwait(false);
            }
        }
        catch (ChannelClosedException)
        {
        }
    }

    private async Task ForEachSubscriberAsync(Func<SubscriptionToken<T>, T, ValueTask<bool>> callback, T state)
    {
        var subscriptionTasks = GetSubscriptionTokens()
            // AsParallel is used to avoid synchronous tasks from clogging things up.
            // It's ok for .ToArray() to take forever, but it's not ok for a callback to wait on another.
            .AsParallel()
            .Select(i => callback(i, state))
            .ToArray();
        foreach (var task in subscriptionTasks)
        {
            await task;
        }
    }

    private static async ValueTask CompleteMessage(T message)
    {
        if (message is null) return;

        if(message is ICompletableMessage completable)
            await completable.CompleteAsync().ConfigureAwait(false);
        await DisposeMessage(message).ConfigureAwait(false);
    }

    private static async ValueTask DisposeMessage(T message)
    {
        switch (message)
        {
            case IAsyncDisposable asyncDisposable:
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                break;
            case IDisposable disposable:
                disposable.Dispose();
                break;
        }
    }

    private async Task RandomTargetMessageProcessor()
    {
        await Task.Yield();
        try
        {
            await foreach (var message in _randomTargetChannel.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                var loopMessage = message;
                // Get random subscription
                var subscriptions = GetSubscriptionTokens()
                    .OrderBy(i => Random.Shared.NextDouble());
                foreach (var subscription in subscriptions)
                {
                    var didProcessMessage = await subscription.TryInvokeListener(loopMessage, SubscriptionMode.Targeted)
                        .ConfigureAwait(false);
                    if (didProcessMessage)
                    {
                        break;
                    }
                }

                await CompleteMessage(message).ConfigureAwait(false);
            }
        }
        catch (ChannelClosedException)
        {
        }
    }

    private ISubscriptionToken AddSubscription(SubscriptionToken<T> subscription)
    {
        _subscriptions.TryAdd(subscription.Id, new WeakReference(subscription));
        InvalidateSubscriptionCaches();
        return subscription;
    }

    private void InvalidateSubscriptionCaches()
    {
        Interlocked.Exchange(ref _weakReferenceCache, null);
        Interlocked.Exchange(ref _subscriptionCache, null);
    }

    public IEnumerable<SubscriptionToken<T>> GetSubscriptionTokens()
    {
        return _subscriptionCache ??=
            EnumerateSubscriptions(_weakReferenceCache ??= _subscriptions.Values.ToArray());
    }

    private IEnumerable<SubscriptionToken<T>> EnumerateSubscriptions(IEnumerable<WeakReference> weakReferences)
    {
        // This is performance critical code, we shouldn't use linq here.
        foreach (var weakReference in weakReferences)
        {
            if (!weakReference.IsAlive)
            {
                continue;
            }

            if (weakReference.Target is not SubscriptionToken<T> token)
            {
                continue;
            }

            yield return token;
        }
    }
}
