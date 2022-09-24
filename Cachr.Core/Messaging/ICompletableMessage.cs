namespace Cachr.Core.Messaging;

public interface ICompletableMessage
{
    ValueTask CompleteAsync();
}
