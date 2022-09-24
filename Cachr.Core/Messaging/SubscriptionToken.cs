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