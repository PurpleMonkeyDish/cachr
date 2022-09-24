namespace Cachr.Core.Messaging;

public interface IMessageBus<T>
    where T : class
{
    Task BroadcastAsync(T message, CancellationToken cancellationToken = default);
    Task SendToRandomAsync(T message, CancellationToken cancellationToken = default);
    ISubscriptionToken Subscribe(ISubscriber<T> subscriber);
    void Unsubscribe(ISubscriptionToken subscriptionToken);
}
