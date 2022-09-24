using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace Cachr.Core.Messaging;

public sealed class MessageBus<T> : IMessageBus<T>, IDisposable
{
    private readonly Channel<T> _broadcastMessages;
    private static readonly bool s_typeIsDisposable = typeof(T).IsAssignableTo(typeof(IDisposable)) || typeof(T).IsAssignableTo(typeof(IAsyncDisposable));
    private static readonly bool s_typeIsICompletableMessage = typeof(T).IsAssignableTo(typeof(ICompletableMessage));
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

    public ISubscriptionToken Subscribe(Func<T, Task> callback)
    {
        return AddSubscription(new SubscriptionToken<T>(callback, this));
    }

    public ISubscriptionToken Subscribe(Func<T, object?, Task> callback, object? state = null)
    {
        return AddSubscription(new SubscriptionToken<T>(callback, this, state));
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
        await Task.Yield();
        try
        {
            await foreach (var message in _broadcastMessages.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                var loopMessage = message;
                var tasks = GetSubscriptionTokens()
                    .Select(i => i.TryInvokeListener(loopMessage))
                    .ToArray();
                if (tasks.Length != 0)
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                await CompleteMessage(message);
            }
        }
        catch (ChannelClosedException)
        {
        }
    }

    private static async ValueTask CompleteMessage(T message)
    {
        if (message is null) return;
        if (!(s_typeIsDisposable || s_typeIsICompletableMessage)) return;
        if (s_typeIsICompletableMessage)
        {
            var completable = (ICompletableMessage)message;
            await completable.CompleteAsync();
        }
        if(s_typeIsDisposable)
            await DisposeMessage(message);
    }
    private static async ValueTask DisposeMessage(T message)
    {
        switch (message)
        {
            case IAsyncDisposable asyncDisposable:
                await asyncDisposable.DisposeAsync();
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
                    var didProcessMessage = await subscription.TryInvokeListener(loopMessage)
                        .ConfigureAwait(false);
                    if (didProcessMessage)
                    {
                        break;
                    }
                }

                await CompleteMessage(message);
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
