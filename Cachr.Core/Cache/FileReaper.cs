using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cachr.Core.Cache;

public class FileReaper : BackgroundService
{
    private readonly IOptions<ReaperConfiguration> _options;
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly ILogger<FileReaper> _logger;

    public FileReaper(IOptions<ReaperConfiguration> options,
        IServiceScopeFactory scopeFactory,
        ILogger<FileReaper> logger)
    {
        _options = options;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var periodicTimer = new PeriodicTimer(_options.Value.ReapInterval);
        try
        {
            do
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                // Injects scoped db context, so we need to resolve it. May as well use a scope per pass.
                var cacheStorage = scope.ServiceProvider.GetRequiredService<ICacheStorage>();
                var start = Stopwatch.GetTimestamp();
                var totalCount = await cacheStorage.ReapAsync(stoppingToken);

                _logger.LogInformation("Reaped a total of {count} record(s) across all shards in {milliseconds}ms",
                    totalCount,
                    Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            } while (await periodicTimer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        catch (Exception ex)
        {
            // This will only happen if we fail to enumerate our storage directory.
            _logger.LogCritical(ex, "FATAL: Reaper process failed due to an exception");
        }
    }
}
