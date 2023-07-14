namespace Cachr.Core.Data;

public interface IDataMapper
{
    CacheEntry? MapCacheEntryData(StoredObject? storedObject);
    IQueryable<CacheEntry> Project(IQueryable<StoredObject> q);
}
