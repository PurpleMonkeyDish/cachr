using Riok.Mapperly.Abstractions;

namespace Cachr.Core.Data;

[Mapper]
public sealed partial class DataMapper : IDataMapper
{
    public partial IQueryable<CacheEntry> Project(IQueryable<StoredObject> q);


    [MapProperty(new[] { nameof(StoredObject.Metadata), nameof(StoredObjectMetadata.Modified) },
        new[] { nameof(CacheEntry.Modified) })]
    [MapProperty(new[] { nameof(StoredObject.Metadata), nameof(StoredObjectMetadata.AbsoluteExpiration) },
        new[] { nameof(CacheEntry.AbsoluteExpiration) })]
    [MapProperty(new[] { nameof(StoredObject.Metadata), nameof(StoredObjectMetadata.SlidingExpiration) },
        new[] { nameof(CacheEntry.SlidingExpiration) })]
    [MapProperty(new[] { nameof(StoredObject.Metadata), nameof(StoredObjectMetadata.LastAccess) },
        new[] { nameof(CacheEntry.LastAccess) })]
    public partial CacheEntry? MapCacheEntryData(StoredObject? storedObject);


    private static DateTimeOffset? MapLongToNullableDateTimeOffset(long? l)
    {
        return l is null ? new DateTimeOffset?() : DateTimeOffset.FromUnixTimeMilliseconds(l.Value);
    }

    private static TimeSpan? MapLongToTimeSpan(double? l)
    {
        return l is null ? new TimeSpan?() : TimeSpan.FromMilliseconds(l.Value);
    }

    private static DateTimeOffset MapLongToDateTimeOffset(long l)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(l);
    }
}
