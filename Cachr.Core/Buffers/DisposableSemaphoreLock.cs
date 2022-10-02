namespace Cachr.Core.Buffers;

public sealed class DisposableSemaphoreLock : IDisposable
{
    private readonly SemaphoreSlim _semaphoreSlim;
    private int _state = 0;

    public DisposableSemaphoreLock(SemaphoreSlim acquiredSemaphore)
    {
        _semaphoreSlim = acquiredSemaphore;
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _state, 1, 0) != 0)
            return;
        _semaphoreSlim.Release();
    }
}