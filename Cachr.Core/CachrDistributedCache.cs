namespace Cachr.Core;

public class CachrDistributedCache
{
    private readonly ICacheBus _cacheBus;

    public CachrDistributedCache(ICacheBus cacheBus)
    {
        _cacheBus = cacheBus;
    }
    public async Task Preload(CancellationToken cancellationToken)
    {
    }
    public byte[] Get(string key)
    {
        throw new NotImplementedException();
    }
}