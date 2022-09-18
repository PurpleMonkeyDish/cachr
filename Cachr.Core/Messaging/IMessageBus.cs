namespace Cachr.Core.Messaging;

public interface IMessageBus<T>
{
    Task BroadcastAsync(T message, CancellationToken cancellationToken = default);
    Task SendToRandomAsync(T message, CancellationToken cancellationToken = default);
    ISubscriptionToken Subscribe(Func<T, Task> callback);
    ISubscriptionToken Subscribe(Func<T, object?, Task> callback, object? state = null);
    void Unsubscribe(ISubscriptionToken subscriptionToken);
}
