using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cachr.Core.Cache;

public class GrimReaper : BackgroundService
{
    private readonly IOptions<ReaperConfiguration> _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICacheFileManager _fileManager;
    private readonly ILogger<GrimReaper> _logger;

    public GrimReaper(IOptions<ReaperConfiguration> options,
        IServiceScopeFactory scopeFactory,
        ICacheFileManager fileManager,
        ILogger<GrimReaper> logger)
    {
        _options = options;
        _scopeFactory = scopeFactory;
        _fileManager = fileManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        var reapCount = _options.Value.ReapPasses;
        if (reapCount <= 0) reapCount = int.MaxValue;
        var batchSize = _options.Value.ReapBatchSize;
        if (batchSize <= 0) batchSize = int.MaxValue;
        using var periodicTimer = new PeriodicTimer(_options.Value.ReapInterval);
        try
        {
            do
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                // Injects scoped db context, so we need to resolve it. May as well use a scope per pass.
                var cacheStorage = scope.ServiceProvider.GetRequiredService<ICacheStorage>();
                _logger.LogInformation("Starting background reap");
                for (var x = 0; x < reapCount; x++)
                {
                    var reapedRecords = await cacheStorage.ReapAsync(batchSize, stoppingToken);
                    _logger.LogInformation("Reaper pass {pass} - Collected {count}", x + 1, reapedRecords);
                    if (reapedRecords == 0) break;
                }

                _logger.LogInformation("Purging empty directories from object storage");
                try
                {
                    _fileManager.PurgeEmptyDirectories(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to purge empty directories. Will continue anyway.");
                }
            } while (await periodicTimer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            // This will only happen if we fail to enumerate our storage directory.
            _logger.LogCritical(ex, "FATAL: Reaper process failed due to an exception");
        }
    }
}
