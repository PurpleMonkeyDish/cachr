using Cachr.Core.Storage;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cachr.Core;

public static class ServiceCollectionExtensions
{
    private static void AddCoreServices(IServiceCollection services)
    {
        services.TryAddSingleton<ICacheStorage, ShardedMemoryCacheStorage>();
        services.TryAddSingleton<ICachrDistributedCache, CachrDistributedCache>();
        services.AddSingleton<IDistributedCache>(s => s.GetRequiredService<ICachrDistributedCache>());        
    }
    public static IServiceCollection AddCachr(this IServiceCollection services, IConfiguration configuration)
    {
        AddCoreServices(services);
        services.Configure<CachrDistributedCacheOptions>(configuration);
        return services;
    }

    public static IServiceCollection AddCachr(this IServiceCollection services,
        Action<CachrDistributedCacheOptions> configureCallback)
    {
        AddCoreServices(services);
        services.Configure<CachrDistributedCacheOptions>(configureCallback);
        return services;
    }
}