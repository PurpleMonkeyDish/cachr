namespace Cachr.Core.Messages.Duplication;

public interface IDuplicateTracker<T>
{
    bool IsDuplicate(T item);
}