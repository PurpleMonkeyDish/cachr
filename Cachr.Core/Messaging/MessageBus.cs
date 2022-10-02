using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace Cachr.Core.Messaging;

public sealed class MessageBus<T> : IMessageBus<T>, IDisposable
    where T : class
{
    private readonly Channel<T> _broadcastMessages;
    private readonly Task _broadcastTask;


    private readonly Channel<T> _randomTargetChannel;

    private readonly ConcurrentDictionary<Guid, WeakReference> _subscriptions =
        new(
            8,
            0
        );

    private IEnumerable<ISubscriber<T>>? _subscriptionCache;
    private WeakReference[]? _weakReferenceCache;
    private IEnumerable<ISubscriber<T>>? _broadcastSubscriptionCache;
    private IEnumerable<ISubscriber<T>>? _targetedSubscriptionCache;

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

    public ISubscriptionToken Subscribe(ISubscriber<T> subscriber)
    {
        if ((subscriber.Mode ^ SubscriptionMode.All) == 0) return subscriber;
        if (!_subscriptions.TryAdd(subscriber.Id, new WeakReference(subscriber))) return subscriber;
        InvalidateSubscriptionCaches();
        return subscriber;
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
        static async ValueTask<bool> Callback(ISubscriber<T> token, T message)
        {
            if (!token.WillTryToHandleMessage(SubscriptionMode.Broadcast, message)) return false;
            return await token.OnMessageAsync(SubscriptionMode.Broadcast, message);
        }

        var broadcastCallback = Callback;
        await Task.Yield();
        try
        {
            var tasks = new List<Task>();
            await foreach (var message in _broadcastMessages.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                tasks.Add(
                    ForEachSubscriberAsync(
                        GetSubscriptionTokens(SubscriptionMode.Broadcast),
                        broadcastCallback,
                        message
                    )
                );
                while (tasks.Any(i => i.IsCompleted) || tasks.Count >= 4)
                {
                    var complete = await Task.WhenAny(tasks).ConfigureAwait(false);
                    await complete;
                    tasks.Remove(complete);
                }
            }

            await Task.WhenAll(tasks);
        }
        catch (ChannelClosedException)
        {
        }
    }
    private static async Task ForEachSubscriberAsync(IEnumerable<ISubscriber<T>> subscriptionTokens,
        Func<ISubscriber<T>, T, ValueTask<bool>> callback, T state, bool completeMessage = true)
    {
        var subscriptionTasks = subscriptionTokens
            // AsParallel is used to avoid synchronous tasks from clogging things up.
            // It's ok for .ToArray() to take forever, but it's not ok for a callback to wait on another.
            .AsParallel()
            .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
            .WithMergeOptions(ParallelMergeOptions.NotBuffered)
            .Select(i => callback(i, state))
            .ToArray();

        foreach (var valueTask in subscriptionTasks)
        {
            await valueTask;
        }

        if (completeMessage)
        {
            CompleteMessage(state);
        }
    }
    private static void CompleteMessage(T? message)
    {
        switch (message)
        {
            case null:
                return;
            case ICompletableMessage completable:
                completable.Complete();
                break;
        }

        DisposeMessage(message);
    }
    private static void DisposeMessage(T message)
    {
        switch (message)
        {
            case IDisposable disposable:
                disposable.Dispose();
                break;
        }
    }
    private async Task RandomTargetMessageProcessor()
    {
        await Task.Yield();

        static async Task ProcessSingleMessageAsync(T message, IEnumerable<ISubscriber<T>> subscriptions)
        {
            // Get random subscription
            foreach (var subscription in subscriptions)
            {
                if (!subscription.WillTryToHandleMessage(SubscriptionMode.Targeted, message)) continue;
                var didProcessMessage = await subscription.OnMessageAsync(SubscriptionMode.Targeted, message)
                    .ConfigureAwait(false);
                if (didProcessMessage)
                {
                    break;
                }
            }

            CompleteMessage(message);
        }

        try
        {
            var tasks = new List<Task>();
            await foreach (var message in _randomTargetChannel.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                var subscriptions = GetSubscriptionTokens(SubscriptionMode.Targeted)
                    .OrderBy(i => Random.Shared.NextDouble());

                tasks.Add(ProcessSingleMessageAsync(message, subscriptions));
            }
        }
        catch (ChannelClosedException)
        {
        }
    }

    private void InvalidateSubscriptionCaches()
    {
        Interlocked.Exchange(ref _weakReferenceCache, null);
        Interlocked.Exchange(ref _subscriptionCache, null);
        Interlocked.Exchange(ref _broadcastSubscriptionCache, null);
        Interlocked.Exchange(ref _targetedSubscriptionCache, null);
    }
    private IEnumerable<ISubscriber<T>> GetCachedAliveSubscriptions(SubscriptionMode mode) =>
        EnumerateSubscriptions(_weakReferenceCache ??= _subscriptions.Values.ToArray(), mode);
    private IEnumerable<ISubscriber<T>> GetSubscriptionTokens(SubscriptionMode mode)
    {
        // Not a valid mode.
        if ((mode ^ SubscriptionMode.All) == 0) return Enumerable.Empty<ISubscriber<T>>();
        return mode switch
        {
            SubscriptionMode.All => GetFromOrRebuildCache(ref _subscriptionCache, mode),
            SubscriptionMode.Broadcast => GetFromOrRebuildCache(ref _broadcastSubscriptionCache, mode),
            SubscriptionMode.Targeted => GetFromOrRebuildCache(ref _targetedSubscriptionCache, mode),
            _ => Enumerable.Empty<ISubscriber<T>>()
        };
    }
    private IEnumerable<ISubscriber<T>> GetFromOrRebuildCache(ref IEnumerable<ISubscriber<T>>? cache, SubscriptionMode mode) =>
        cache ??= GetCachedAliveSubscriptions(mode);
    private IEnumerable<ISubscriber<T>> EnumerateSubscriptions(WeakReference[] weakReferences,
        SubscriptionMode mode = SubscriptionMode.All)
    {
        // This is performance critical code, we shouldn't use linq here.
        for (var x = 0; x < weakReferences.Length; x++)
        {
            var weakReference = weakReferences[x];
            if (!weakReference.IsAlive || weakReference.Target is not ISubscriber<T> token)
            {
                continue;
            }

            if (!token.HandlesMode(mode)) continue;

            yield return token;
        }
    }
}
