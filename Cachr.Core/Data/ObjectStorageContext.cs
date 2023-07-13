using System.ComponentModel.DataAnnotations.Schema;
using Cachr.Core.Data.Storage;
using Microsoft.EntityFrameworkCore;

namespace Cachr.Core.Data;

public class StoredObject
{
    public required string Key { get; set; }
    public Guid MetadataId { get; set; }
    public long Created { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    public long LastUpdate { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
}
public class ObjectMetadata
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public long? AbsoluteExpiration { get; init; }
    public long? SlidingExpiration { get; init; }
    public long? CurrentExpiration { get; init; }
}

public class ObjectStorageContext : DbContext
{
    public static string ConnectionString { get; set; } = "Data Source=./data.db";

    public ObjectStorageContext(DbContextOptions<ObjectStorageContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StoredObject>().Property(i => i.Key).IsRequired().ValueGeneratedNever();
        modelBuilder.Entity<StoredObject>().HasKey(i => i.Key);
        modelBuilder.Entity<StoredObject>().HasIndex(i => i.MetadataId).IsUnique();
        modelBuilder.Entity<StoredObject>().Property(i => i.MetadataId).IsRequired();
        modelBuilder.Entity<StoredObject>().HasOne<ObjectMetadata>();

        modelBuilder.Entity<ObjectMetadata>().HasKey(i => i.Id);
        modelBuilder.Entity<ObjectMetadata>().Property(i => i.Id).IsRequired().ValueGeneratedNever();
        base.OnModelCreating(modelBuilder);
    }

    public DbSet<StoredObject> StoredObjects { get; set; }
    public DbSet<ObjectMetadata> ObjectMetadata { get; set; }
}
