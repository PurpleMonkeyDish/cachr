using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Cachr.Core.Data;

public class StorageObjectConfiguration
{
    public string ConnectionString { get; set; } = "Data Source=./objects.db";
}

public record CacheEntryData
{
    public string Key { get; init; }
    public Guid MetadataId { get; init; }
    public DateTimeOffset Created { get; init; }
    public DateTimeOffset LastModified { get; init; }
    public DateTimeOffset? AbsoluteExpiration { get; init; }
    public TimeSpan? SlidingExpiration { get; init; }
}

internal class StoredObject
{
    public required string Key { get; set; }
    public Guid MetadataId { get; set; }
    public long Created { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    public long Modified { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
}

internal class StoredObjectMetadata
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public long? AbsoluteExpiration { get; init; }
    public long? SlidingExpiration { get; init; }
    public long? CurrentExpiration { get; init; }
    public DateTimeOffset Created { get; init; }
    public DateTimeOffset Modified { get; init; }
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
        modelBuilder.Entity<StoredObject>().HasOne<StoredObjectMetadata>();

        modelBuilder.Entity<StoredObjectMetadata>().HasKey(i => i.Id);
        modelBuilder.Entity<StoredObjectMetadata>().Property(i => i.Id).IsRequired().ValueGeneratedNever();
        base.OnModelCreating(modelBuilder);
    }

    internal DbSet<StoredObject> StoredObjects { get; set; }
    internal DbSet<StoredObjectMetadata> ObjectMetadata { get; set; }
}
