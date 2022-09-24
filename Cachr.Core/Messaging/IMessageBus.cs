namespace Cachr.Core.Messaging;

public interface IMessageBus<T>
    where T : class
{
    Task BroadcastAsync(T message, CancellationToken cancellationToken = default);
    Task SendToRandomAsync(T message, CancellationToken cancellationToken = default);
    ISubscriptionToken Subscribe(ISubscriber<T> subscriber);
    void Unsubscribe(ISubscriptionToken subscriptionToken);
}

public static class MessageBusExtensions
{
    public static ISubscriptionToken Subscribe<T>(this IMessageBus<T> messageBus, Func<T, ValueTask> callback,
        SubscriptionMode mode = SubscriptionMode.All)
        where T : class
    {
        var subscription = new SubscriptionToken<T>(callback, messageBus, mode);
        return messageBus.Subscribe(subscription);
    }

    public static ISubscriptionToken Subscribe<T>(this IMessageBus<T> messageBus, Func<T, object?, ValueTask> callback,
        object? state = null, SubscriptionMode mode = SubscriptionMode.All)
    where T : class
    {
        var subscription = new SubscriptionToken<T>(callback, messageBus, state, mode);
        return messageBus.Subscribe(subscription);
    }
}
