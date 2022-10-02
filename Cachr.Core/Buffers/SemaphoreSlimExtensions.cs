namespace Cachr.Core.Buffers;

public static class SemaphoreSlimExtensions
{
    public static async Task<IDisposable> AcquireAsync(this SemaphoreSlim semaphoreSlim,
        CancellationToken cancellationToken)
    {
        await semaphoreSlim.WaitAsync(cancellationToken);
        return new DisposableSemaphoreLock(semaphoreSlim);
    }
}