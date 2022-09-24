namespace Cachr.Core.Messaging;

public interface IMessageBus<T>
{
    Task BroadcastAsync(T message, CancellationToken cancellationToken = default);
    Task SendToRandomAsync(T message, CancellationToken cancellationToken = default);
    ISubscriptionToken Subscribe(Func<T, ValueTask> callback, bool broadcast = true, bool targeted = true);
    ISubscriptionToken Subscribe(Func<T, object?, ValueTask> callback, object? state = null, bool broadcast = true, bool targeted = true);
    void Unsubscribe(ISubscriptionToken subscriptionToken);
}
