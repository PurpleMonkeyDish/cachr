namespace Cachr.Core.Messaging;

public sealed class SubscriptionToken<T> : ISubscriptionToken, IDisposable, IAsyncDisposable
{
    private Func<T, object?, ValueTask>? Callback { get; }
    private readonly IMessageBus<T> _messageBus;
    private readonly object? _state;
    private volatile int _disposedState;

    public SubscriptionMode Mode { get; }

    public SubscriptionToken(Func<T, ValueTask> callback, IMessageBus<T> messageBus, SubscriptionMode mode) :
        this(CreateCallbackWrapper(callback), messageBus, null, mode)
    {
    }

    public SubscriptionToken(Func<T, object?, ValueTask> callback, IMessageBus<T> messageBus, object? state,
        SubscriptionMode mode)
    {
        ArgumentNullException.ThrowIfNull(callback);
        Callback = callback;
        _state = state;
        _messageBus = messageBus;
        Mode = mode;
    }

    private bool IsDisposed => _disposedState != 0;

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }

    public Guid Id { get; } = Guid.NewGuid();

    private static Func<T, object?, ValueTask> CreateCallbackWrapper(Func<T, ValueTask> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return (message, _) => callback.Invoke(message);
    }

    public async ValueTask<bool> TryInvokeListener(T message, SubscriptionMode mode)
    {
        switch (mode)
        {
            case SubscriptionMode.Broadcast when IsDisposed || !Mode.HasFlag(SubscriptionMode.Broadcast):
            case SubscriptionMode.Targeted when IsDisposed || !Mode.HasFlag(SubscriptionMode.Targeted):
            case SubscriptionMode.All when IsDisposed:
                return false;
            case SubscriptionMode.All when !Mode.HasFlag(SubscriptionMode.Broadcast) && !Mode.HasFlag(SubscriptionMode.Targeted):
                return false;
        }

        if (IsDisposed)
        {
            return false;
        }

        var callback = Callback;

        if (callback is null)
        {
            await ((ISubscriptionToken)this).UnsubscribeAsync().ConfigureAwait(false);
            return false;
        }

        try
        {
            await callback.Invoke(message, _state).ConfigureAwait(false);
            return true;
        }
        catch (Exception)
        {
            await ((ISubscriptionToken)this).UnsubscribeAsync().ConfigureAwait(false);
            return false;
        }
    }

#pragma warning disable CS1998
    public async ValueTask DisposeAsync()

    {
        if (Interlocked.CompareExchange(ref _disposedState, 1, 0) == 1)
        {
            return;
        }

        _messageBus.Unsubscribe(this);
    }
#pragma warning restore CS1998
}
