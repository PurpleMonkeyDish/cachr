using Cachr.Core.Discovery;
using Cachr.Core.Messages.Duplication;
using Cachr.Core.Messaging;
using Cachr.Core.Peering;
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
        services.TryAddSingleton<IPeerDiscoveryProvider, DefaultPeerDiscoveryProvider>();
        services.AddSingleton<IDistributedCache>(s => s.GetRequiredService<ICachrDistributedCache>());
        services.AddSingleton(typeof(IMessageBus<>), typeof(MessageBus<>));
        services.AddSingleton(typeof(IDuplicateTracker<>), typeof(DuplicateTracker<>));
        services.AddSingleton<IPeerSelector, PeerSelector>();
        services.AddSingleton<IPeerStatusTracker, PeerStatusTracker>();
    }

    public static IServiceCollection AddCachr(this IServiceCollection services, IConfiguration configuration)
    {
        AddCoreServices(services);
        services.Configure<CachrDistributedCacheOptions>(configuration);
        return services;
    }

    public static IServiceCollection AddCachr(this IServiceCollection services,
        Action<CachrDistributedCacheOptions> configureAction)
    {
        AddCoreServices(services);
        services.Configure(configureAction);
        return services;
    }

    public static IServiceCollection AddStaticPeerDiscovery(this IServiceCollection services, Action<StaticPeerConfiguration> configureAction)
    {
        services.Configure(configureAction);
        services.AddSingleton<IPeerDiscoveryProvider, StaticPeerDiscoveryProvider>();
        return services;
    }


    public static IServiceCollection AddStaticPeerDiscovery(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StaticPeerConfiguration>(configuration);
        services.AddSingleton<IPeerDiscoveryProvider, StaticPeerDiscoveryProvider>();
        return services;
    }
}
