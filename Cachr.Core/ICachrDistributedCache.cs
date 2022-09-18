using Microsoft.Extensions.Caching.Distributed;

namespace Cachr.Core;

public interface ICachrDistributedCache : IDistributedCache
{
    void BeginPreload();
}
