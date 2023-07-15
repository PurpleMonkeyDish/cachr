using Cachr.Core.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cachr.Core.Cache;

public class CacheFileManager : ICacheFileManager
{
    private const FileShare ShareMode = FileShare.ReadWrite | FileShare.Delete;
    private readonly IOptions<StorageObjectConfiguration> _options;
    private readonly ILogger<CacheFileManager> _logger;

    public CacheFileManager(IOptions<StorageObjectConfiguration> options, ILogger<CacheFileManager> logger)
    {
        _options = options;
        var directory = Directory.CreateDirectory(BasePath);
        logger.LogInformation("Cache directory: {path}", directory.FullName);
        _logger = logger;
    }

    public string FileName => "object.bin";

    public string BasePath => _options.Value.BasePath;

    public void PurgeShard(int shard)
    {
        var target = Path.GetFullPath(Path.Combine(BasePath, shard.ToString()));
        if (Directory.Exists(target))
        {
            Directory.Delete(target, true);
        }
    }

    public Stream Open(Guid id, int shard, bool readOnly)
    {
        var path = GetPath(id, shard);
        return readOnly
            ? File.Open(path, FileMode.Open, FileAccess.Read, ShareMode)
            : File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, ShareMode);
    }

    public void Delete(Guid id, int shard)
    {
        var path = GetPath(id, shard);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public void PurgeEmptyDirectories(CancellationToken cancellationToken)
    {
        var directory = Directory.CreateDirectory(BasePath);
        _logger.LogInformation("Scanning for empty directories in {path}", directory.FullName);
        var directoriesToProcess = new Stack<DirectoryInfo>();
        var purgedCount = 0;
        var earliestDateTime = DateTime.Now.AddMinutes(-30);
        directoriesToProcess.Push(directory);
        while (directoriesToProcess.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var next = directoriesToProcess.Pop();
            next.Refresh();
            if (next.CreationTime > earliestDateTime) continue;
            if (!next.Exists) continue;
            var innerObjects = next.GetFileSystemInfos();
            if (innerObjects.Length == 0)
            {
                purgedCount++;
                if (next.FullName != directory.FullName)
                {
                    next.Delete();
                }

                continue;
            }

            foreach (var info in innerObjects.OfType<DirectoryInfo>())
            {
                cancellationToken.ThrowIfCancellationRequested();
                directoriesToProcess.Push(info);
            }
        }
        _logger.LogInformation("Purged {count} empty directories from object store", purgedCount);
    }

    private string GetPath(Guid id, int shard)
    {
        var objectDirectory = id.ToString("n");
        var path = Path.GetFullPath(Path.Combine(_options.Value.BasePath,
            shard.ToString(),
            objectDirectory[..2],
            objectDirectory[2..4],
            objectDirectory));
        var directory = Directory.CreateDirectory(path);
        return Path.Combine(directory.FullName, FileName);
    }
}
