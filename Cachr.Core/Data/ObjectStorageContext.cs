using Microsoft.EntityFrameworkCore;

namespace Cachr.Core.Data;

public class ObjectStorageContext : DbContext
{
    public ObjectStorageContext(DbContextOptions<ObjectStorageContext> options)
        : base(options)
    {
    }

    public DbSet<StoredObject> StoredObjects { get; set; }
    public DbSet<StoredObjectMetadata> ObjectMetadata { get; set; }

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
}
