using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;

namespace Cachr.Core.Data;

public class StorageObjectConfiguration
{
    public string ConnectionString { get; set; } = "Data Source=./objects.db";
}

public record CacheEntry
{
    public required string Key { get; init; }
    public Guid MetadataId { get; init; }
    public DateTimeOffset Created { get; init; }
    public DateTimeOffset Modified { get; init; }
    public DateTimeOffset? AbsoluteExpiration { get; init; }
    public TimeSpan? SlidingExpiration { get; init; }
}

internal interface IDataMapper
{
    CacheEntry MapCacheEntryData(StoredObject storedObject);
}

[Mapper]
internal static partial class DataMapperExtensions
{
    public static partial IQueryable<CacheEntry> Project(this IQueryable<StoredObject> q);
    [MapProperty(new[] { nameof(StoredObject.Metadata), nameof(StoredObjectMetadata.Modified) },
        new[] { nameof(CacheEntry.Modified) })]
    [MapProperty(new[] { nameof(StoredObject.Metadata), nameof(StoredObjectMetadata.AbsoluteExpiration) },
        new[] { nameof(CacheEntry.AbsoluteExpiration) })]
    [MapProperty(new[] { nameof(StoredObject.Metadata), nameof(StoredObjectMetadata.SlidingExpiration) },
        new[] { nameof(CacheEntry.SlidingExpiration) })]
    private static partial CacheEntry ProjectToCacheEntry(StoredObject storedObject);
}

[Mapper]
internal sealed partial class DataMapper : IDataMapper
{

    [MapProperty(new[] { nameof(StoredObject.Metadata), nameof(StoredObjectMetadata.Modified) },
        new[] { nameof(CacheEntry.Modified) })]
    [MapProperty(new[] { nameof(StoredObject.Metadata), nameof(StoredObjectMetadata.AbsoluteExpiration) },
        new[] { nameof(CacheEntry.AbsoluteExpiration) })]
    [MapProperty(new[] { nameof(StoredObject.Metadata), nameof(StoredObjectMetadata.SlidingExpiration) },
        new[] { nameof(CacheEntry.SlidingExpiration) })]
    public partial CacheEntry MapCacheEntryData(StoredObject storedObject);
}

internal class StoredObject
{
    public required string Key { get; set; }
    public Guid MetadataId { get; set; }
    public DateTimeOffset Created { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset Modified { get; set; } = DateTimeOffset.Now;
    public virtual required StoredObjectMetadata Metadata { get; set; }
}

internal class StoredObjectMetadata
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset? AbsoluteExpiration { get; set; }
    public TimeSpan? SlidingExpiration { get; set; }
    public DateTimeOffset? CurrentExpiration { get; set; }
    public DateTimeOffset Created { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset Modified { get; set; } = DateTimeOffset.Now;
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
        modelBuilder.Entity<StoredObject>().HasOne<StoredObjectMetadata>(i => i.Metadata).WithOne();

        modelBuilder.Entity<StoredObjectMetadata>().HasKey(i => i.Id);
        modelBuilder.Entity<StoredObjectMetadata>().Property(i => i.Id).IsRequired().ValueGeneratedNever();
        base.OnModelCreating(modelBuilder);
    }

    internal DbSet<StoredObject> StoredObjects { get; set; }
    internal DbSet<StoredObjectMetadata> ObjectMetadata { get; set; }
}
