using System.Collections;
using Cachr.Core.Discovery;
using Cachr.Core.Messaging;
using Cachr.Core.Serializers;
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
        // Try add is used for services we expect to be replaced, before or after we're called.
        services.TryAddSingleton<IPeerDiscoveryProvider, DefaultPeerDiscoveryProvider>();
        services.AddSingleton<ICacheStorage, ShardedMemoryCacheStorage>();
        services.AddSingleton<ICachrDistributedCache, CachrDistributedCache>();
        services.AddSingleton<IDistributedCache>(static s => s.GetRequiredService<ICachrDistributedCache>());
        services.AddSingleton(typeof(IMessageBus<>), typeof(MessageBus<>));
        services.AddSingleton(typeof(ISerializer<>), typeof(DefaultSerializer<>));
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

    public static IServiceCollection AddStaticPeerDiscovery(this IServiceCollection services,
        Action<StaticPeerConfiguration> configureAction)
    {
        services.Configure(configureAction);
        services.AddSingleton<IPeerDiscoveryProvider, StaticPeerDiscoveryProvider>();
        return services;
    }


    public static IServiceCollection AddStaticPeerDiscovery(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<StaticPeerConfiguration>(configuration);
        services.AddSingleton<IPeerDiscoveryProvider, StaticPeerDiscoveryProvider>();
        return services;
    }


    public static IServiceCollection AddDnsDiscovery(this IServiceCollection services,
        Action<DnsDiscoveryConfiguration> configureAction)
    {
        services.Configure(configureAction);
        services.AddSingleton<IPeerDiscoveryProvider, DnsPeerDiscoveryProvider>();
        return services;
    }


    public static IServiceCollection AddDnsDiscovery(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DnsDiscoveryConfiguration>(configuration);
        services.AddSingleton<IPeerDiscoveryProvider, DnsPeerDiscoveryProvider>();
        return services;
    }


    internal static void RegisterTypeAsInterfaces<TImplementation>(this IServiceCollection services,
        Type[]? interfaceTypes = null,
        ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
        where TImplementation : class
    {
        if (services.Any(i => i.ServiceType == typeof(TImplementation))) return;
        interfaceTypes ??= Array.Empty<Type>();
        if (typeof(TImplementation).IsAbstract || typeof(TImplementation).IsPrimitive)
            services.Add(ServiceDescriptor.Describe(typeof(TImplementation),
                typeof(TImplementation),
                serviceLifetime));

        if (interfaceTypes.Length == 0)
        {
            interfaceTypes = typeof(TImplementation).GetInterfaces();
            var interfaceTypesQuery = interfaceTypes.WhereNot(static i => i.IsGenericType)
                .WhereNot(static i =>
                    i == typeof(IDisposable) ||
                    i == typeof(IEnumerable) ||
                    i == typeof(IAsyncDisposable) ||
                    i == typeof(ISubscriptionToken)
                );

            var genericInterfaceTypesQuery = interfaceTypes.Where(i => i.IsGenericType)
                .Select(t => (Type: t, GenericDefinition: t.GetGenericTypeDefinition()))
                .WhereNot(i => i.GenericDefinition == typeof(ISubscriber<>))
                .Select(i => i.Type);


            interfaceTypes = interfaceTypesQuery.Union(genericInterfaceTypesQuery).ToArray();
        }

        Func<IServiceProvider, object> factory = s => s.GetRequiredService<TImplementation>();
        var exceptions = new List<Exception>();
        foreach (var interfaceType in interfaceTypes)
        {
            if (!typeof(TImplementation).IsAssignableTo(interfaceType))
            {
                exceptions.Add(new InvalidOperationException(
                    $"Type {typeof(TImplementation).FullName} is not assignable to {interfaceType.FullName}"));
                continue;
            }


            services.Add(ServiceDescriptor.Describe(interfaceType, factory, serviceLifetime));
        }

        if (exceptions.Count == 1) throw exceptions.Single();
        if (exceptions.Count > 0)
            throw new AggregateException(
                $"Unable to register {typeof(TImplementation).FullName}, see inner exceptions for more detail.",
                exceptions
            );
    }

    internal static IEnumerable<T> WhereNot<T>(this IEnumerable<T> enumerable, Func<T, bool> evaluator)
    {
        return enumerable.Where(i => !evaluator.Invoke(i));
    }

    internal static IEnumerable<(T1 Left, T2 Right)> PairWith<T1, T2>(this IEnumerable<T1> enumerable, T2 item)
    {
        foreach (var enumerableItem in enumerable)
        {
            yield return (enumerableItem, item);
        }
    }
}
