namespace Cachr.Core.Messaging;

public interface ISubscriptionToken : IDisposable
{
    Guid Id { get; }
    void Unsubscribe();
}
