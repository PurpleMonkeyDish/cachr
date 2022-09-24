namespace Cachr.Core.Messaging;

public interface ISubscriptionToken : IDisposable, IAsyncDisposable
{
    Guid Id { get; }
    void Unsubscribe() => Dispose();
    ValueTask UnsubscribeAsync() => DisposeAsync();
}
