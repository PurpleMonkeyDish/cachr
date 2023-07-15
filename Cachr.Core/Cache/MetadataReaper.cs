using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cachr.Core.Cache;

public class MetadataReaper : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MetadataReaper> _logger;

    public MetadataReaper(IServiceScopeFactory scopeFactory, ILogger<MetadataReaper> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var periodicTimer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await periodicTimer.WaitForNextTickAsync(stoppingToken))
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var cacheStorage = scope.ServiceProvider.GetRequiredService<ICacheStorage>();
            var start = Stopwatch.GetTimestamp();
            var count = await cacheStorage.ReapStaleMetadataAsync(stoppingToken);
            _logger.LogInformation("Reaped {count} stale metadata records in {milliseconds}ms", count, Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        }
    }
}