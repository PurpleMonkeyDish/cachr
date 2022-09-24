namespace Cachr.Core.Messaging;

public interface IMessageBus<T>
{
    Task BroadcastAsync(T message, CancellationToken cancellationToken = default);
    Task SendToRandomAsync(T message, CancellationToken cancellationToken = default);
    ISubscriptionToken Subscribe(Func<T, ValueTask> callback, SubscriptionMode mode = SubscriptionMode.All);
    ISubscriptionToken Subscribe(Func<T, object?, ValueTask> callback, object? state = null, SubscriptionMode mode = SubscriptionMode.All);
    void Unsubscribe(ISubscriptionToken subscriptionToken);
}
