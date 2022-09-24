namespace Cachr.Core.Messaging;

public sealed class SubscriptionToken<T> : ISubscriptionToken
{
    private readonly Func<T, object?, Task> _callback;
    private readonly IMessageBus<T> _messageBus;
    private readonly object? _state;
    private volatile int _disposedState;

    public SubscriptionToken(Func<T, Task> callback, IMessageBus<T> messageBus) :
        this(CreateCallbackWrapper(callback), messageBus)
    {
    }

    public SubscriptionToken(Func<T, object?, Task> callback, IMessageBus<T> messageBus, object? state = null)
    {
        ArgumentNullException.ThrowIfNull(callback);
        _callback = callback;
        _state = state;
        _messageBus = messageBus;
    }

    private bool IsDisposed => _disposedState != 0;

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }

    public Guid Id { get; } = Guid.NewGuid();

    public void Unsubscribe()
    {
        Dispose();
    }

    private static Func<T, object?, Task> CreateCallbackWrapper(Func<T, Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return (message, _) => callback.Invoke(message);
    }

    public async Task<bool> TryInvokeListener(T message, bool broadcast = true)
    {
        if (IsDisposed)
        {
            return false;
        }

        try
        {
            await _callback.Invoke(message, _state).ConfigureAwait(false);
            return true;
        }
        catch (Exception)
        {
            await UnsubscribeAsync().ConfigureAwait(false);
            return false;
        }
    }

    public ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _disposedState, 1, 0) == 1)
        {
            return new ValueTask();
        }

        _messageBus.Unsubscribe(this);
        return new ValueTask();
    }

    public ValueTask UnsubscribeAsync()
    {
        return DisposeAsync();
    }
}
