using Cachr.Core.Data;

namespace Cachr.Core.Cache;

public interface ICacheFileManager
{
    public string BasePath { get; }
    public string FileName { get; }

    Stream Open(CacheEntry entry, bool readOnly)
    {
        return Open(entry.MetadataId, entry.Shard, readOnly);
    }

    Stream Open(Guid id, int shard, bool readOnly);

    void Delete(CacheEntry entry)
    {
        Delete(entry.MetadataId, entry.Shard);
    }

    void Delete(Guid id, int shard);
    void PurgeShard(int shard);
    void PurgeEmptyDirectories(CancellationToken cancellationToken);
}
