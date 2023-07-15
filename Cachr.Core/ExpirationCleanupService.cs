using Cachr.Core.Cache;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cachr.Core;

public class ExpirationCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExpirationCleanupService> _logger;

    public ExpirationCleanupService(IServiceProvider serviceProvider, ILogger<ExpirationCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var periodicTimer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        while (await periodicTimer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var cacheStorage = scope.ServiceProvider.GetRequiredService<ICacheStorage>();
            _logger.LogInformation("Reaping expired records");
            await cacheStorage.ReapExpiredRecordsAsync(stoppingToken).ConfigureAwait(false);
        }
    }
}