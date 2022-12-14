using Cachr.Core;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Cachr.UnitTests;

public sealed class ServiceCollectionTests
{
    [Fact]
    public void CachrCanBeResolvedSuccessfullyWhenRegisteredWithOptionLambdaConstructor()
    {
        var services = GetServiceCollection();
        services.AddCachr(o => { });

        using var serviceProvider = services.BuildServiceProvider();
        var cachrInterface = serviceProvider.GetRequiredService<ICachrDistributedCache>();
        var frameworkInterface = serviceProvider.GetRequiredService<IDistributedCache>();

        Assert.Same(cachrInterface, frameworkInterface);
    }

    [Fact]
    public void CachrCanBeResolvedSuccessfullyWhenRegisteredWithIConfiguration()
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection();
        var configuration = configurationBuilder.Build();
        var services = GetServiceCollection();
        services.AddCachr(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var cachrInterface = serviceProvider.GetRequiredService<ICachrDistributedCache>();
        var frameworkInterface = serviceProvider.GetRequiredService<IDistributedCache>();

        Assert.Same(cachrInterface, frameworkInterface);
    }

    [Fact]
    public void AllServicesCanBeResolved()
    {
        var serviceCollection = GetServiceCollection();
        serviceCollection.AddCachr(c => { });
        using var serviceProvider = serviceCollection.BuildServiceProvider();
        foreach (var serviceDescriptor in serviceCollection)
        {
            if (serviceDescriptor.ServiceType.IsGenericTypeDefinition) continue;
            var result = serviceProvider.GetService(serviceDescriptor.ServiceType);
            Assert.NotNull(result);
        }
    }

    private static IServiceCollection GetServiceCollection()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        serviceCollection.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        return serviceCollection;
    }
}
