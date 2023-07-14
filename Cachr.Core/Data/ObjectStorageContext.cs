using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;

namespace Cachr.Core.Data;

public class StorageObjectConfiguration
{
    public string ConnectionString { get; set; } = "Data Source=./objects.db";
    public string BasePath { get; set; } = "./data";
}

public record CacheEntry
{
    public required string Key { get; init; }
    public Guid MetadataId { get; init; }
    public int Shard { get; init; }
    public DateTimeOffset Created { get; init; }
    public DateTimeOffset Modified { get; init; }
    public DateTimeOffset? AbsoluteExpiration { get; init; }
    public TimeSpan? SlidingExpiration { get; init; }
    public DateTimeOffset LastAccess { get; init; }
}

public static class DataMapperExtensions
{
}

public interface IDataMapper
{
    CacheEntry? MapCacheEntryData(StoredObject? storedObject);
    IQueryable<CacheEntry> Project(IQueryable<StoredObject> q);
}

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


    private static DateTimeOffset? MapLongToNullableDateTimeOffset(long? l) =>
        l is null ? new DateTimeOffset?() : DateTimeOffset.FromUnixTimeMilliseconds(l.Value);

    private static TimeSpan? MapLongToTimeSpan(double? l) =>
        l is null ? new TimeSpan?() : TimeSpan.FromMilliseconds(l.Value);

    private static DateTimeOffset MapLongToDateTimeOffset(long l) => DateTimeOffset.FromUnixTimeMilliseconds(l);
}

public class StoredObject
{
    public required string Key { get; set; }
    public required int Shard { get; set; }
    public required Guid MetadataId { get; set; }
    public long Created { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    public long Modified { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    // We should just be able to create the MetadataId, this will only be used for Querying.
    public virtual StoredObjectMetadata Metadata { get; set; } = null!;
}

public class StoredObjectMetadata
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long? AbsoluteExpiration { get; set; }
    public double? SlidingExpiration { get; set; }
    public long Created { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    public long Modified { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    public long LastAccess { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
}

public class ObjectStorageContext : DbContext
{
    public ObjectStorageContext(DbContextOptions<ObjectStorageContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StoredObject>().Property(i => i.Key).IsRequired().ValueGeneratedNever();
        modelBuilder.Entity<StoredObject>().HasKey(i => i.Key);
        modelBuilder.Entity<StoredObject>().HasIndex(i => i.MetadataId).IsUnique();
        modelBuilder.Entity<StoredObject>().Property(i => i.MetadataId).IsRequired();
        modelBuilder.Entity<StoredObject>().HasOne<StoredObjectMetadata>(i => i.Metadata);

        modelBuilder.Entity<StoredObjectMetadata>().HasKey(i => i.Id);
        modelBuilder.Entity<StoredObjectMetadata>().Property(i => i.Id).IsRequired().ValueGeneratedNever();
        base.OnModelCreating(modelBuilder);
    }

    public DbSet<StoredObject> StoredObjects { get; set; }
    public DbSet<StoredObjectMetadata> ObjectMetadata { get; set; }
}
