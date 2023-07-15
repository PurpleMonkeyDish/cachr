using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cachr.Core.Cache;

public class EmptyDirectoryReaper : BackgroundService
{
    private readonly ICacheFileManager _fileManager;
    private readonly ILogger<EmptyDirectoryReaper> _logger;

    public EmptyDirectoryReaper(ICacheFileManager fileManager, ILogger<EmptyDirectoryReaper> logger)
    {
        _fileManager = fileManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (true)
        {
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
                _logger.LogInformation("Purging empty directories from object storage");
                _fileManager.PurgeEmptyDirectories(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to purge empty directories. Will continue anyway.");
            }
        }
    }
}