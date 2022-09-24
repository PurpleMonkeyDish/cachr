namespace Cachr.Core.Messaging;

public sealed class SubscriptionToken<T> : SubscriptionBase<T>
    where T : class
{
    private Func<T, object?, ValueTask> Callback { get; }

    protected override ValueTask ProcessMessageAsync(SubscriptionMode mode, T message, object? state)
    {
        return Callback.Invoke(message, state);
    }

    public SubscriptionToken(Func<T, ValueTask> callback, IMessageBus<T> messageBus, SubscriptionMode mode) :
        this(CreateCallbackWrapper(callback), messageBus, null, mode)
    {
    }

    public SubscriptionToken(Func<T, object?, ValueTask> callback, IMessageBus<T> messageBus, object? state,
        SubscriptionMode mode)
        : base (messageBus, mode, state)
    {
        ArgumentNullException.ThrowIfNull(callback);
        Callback = callback;
    }


    private static Func<T, object?, ValueTask> CreateCallbackWrapper(Func<T, ValueTask> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return (message, _) => callback.Invoke(message);
    }
}

public abstract class SubscriptionBase<T> : ISubscriber<T>
    where T : class
{
    protected IMessageBus<T> MessageBus { get; }
    private readonly object? _state;
    private volatile int _disposedState;
    protected SubscriptionBase(IMessageBus<T> messageBus, SubscriptionMode mode, object? state)
    {
        MessageBus = messageBus;
        Mode = mode;
        _state = state;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public SubscriptionMode Mode { get; }

    public bool HandlesMode(SubscriptionMode mode) => Mode == SubscriptionMode.All || Mode.HasFlag(mode);

    public bool WillTryToHandleMessage(SubscriptionMode mode, T message)
    {
        if (IsDisposed) return false;
        return CanHandleMessage(mode, message);
    }

    protected virtual bool CanHandleMessage(SubscriptionMode mode, T message) => true;

    public async ValueTask<bool> OnMessageAsync(SubscriptionMode mode, T message)
    {
        if (IsDisposed) return false;
        try
        {
            await ProcessMessageAsync(mode, message, _state);
            return true;
        }
        catch
        {
            await DisposeAsync();
            return false;
        }
    }

    protected abstract ValueTask ProcessMessageAsync(SubscriptionMode mode, T message, object? state);

    private bool IsDisposed => _disposedState != 0;

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }
#pragma warning disable CS1998
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _disposedState, 1, 0) == 1)
        {
            return;
        }

        MessageBus.Unsubscribe(this);
    }
#pragma warning restore CS1998
}
