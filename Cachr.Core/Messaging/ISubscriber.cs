namespace Cachr.Core.Messaging;

public interface ISubscriber<in T> : ISubscriptionToken
    where T : class
{
    SubscriptionMode Mode { get; }
    bool HandlesMode(SubscriptionMode mode) => Mode == SubscriptionMode.All || Mode.HasFlag(mode);
    bool WillTryToHandleMessage(SubscriptionMode mode, T message) => true;
    ValueTask<bool> OnMessageAsync(SubscriptionMode mode, T message);
}