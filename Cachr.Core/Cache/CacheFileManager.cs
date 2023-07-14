using Cachr.Core.Data;
using Microsoft.Extensions.Options;

namespace Cachr.Core.Cache;

public class CacheFileManager : ICacheFileManager
{
    private const FileShare ShareMode = FileShare.ReadWrite | FileShare.Delete;
    private readonly IOptions<StorageObjectConfiguration> _options;

    public CacheFileManager(IOptions<StorageObjectConfiguration> options)
    {
        _options = options;
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
