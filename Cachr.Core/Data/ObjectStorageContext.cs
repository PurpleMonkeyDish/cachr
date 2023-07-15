using Microsoft.EntityFrameworkCore;

namespace Cachr.Core.Data;

public sealed class ObjectStorageContext : DbContext
{
    public ObjectStorageContext(DbContextOptions<ObjectStorageContext> options)
        : base(options)
    {;
    }

    public DbSet<StoredObject> StoredObjects { get; set; }
    public DbSet<StoredObjectMetadata> ObjectMetadata { get; set; }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        var ret = base.SaveChanges(acceptAllChangesOnSuccess);
        this.ChangeTracker.Clear();
        return ret;
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken())
    {
        var ret = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        this.ChangeTracker.Clear();
        return ret;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StoredObject>().Property(i => i.Key).IsRequired().ValueGeneratedNever();
        modelBuilder.Entity<StoredObject>().HasKey(i => i.Key);
        modelBuilder.Entity<StoredObject>().HasIndex(i => i.MetadataId).IsUnique();
        modelBuilder.Entity<StoredObject>().HasIndex(i => i.Shard).IsUnique(false);
        modelBuilder.Entity<StoredObject>().HasIndex(i => new { i.Shard, i.Key }).IsUnique();
        modelBuilder.Entity<StoredObject>().HasIndex(i => new { i.Shard, i.MetadataId }).IsUnique();
        modelBuilder.Entity<StoredObject>().Property(i => i.MetadataId).IsRequired();
        modelBuilder.Entity<StoredObject>().HasOne<StoredObjectMetadata>(i => i.Metadata);

        modelBuilder.Entity<StoredObjectMetadata>().HasKey(i => i.Id);
        modelBuilder.Entity<StoredObjectMetadata>().Property(i => i.Id).IsRequired().ValueGeneratedNever();
        base.OnModelCreating(modelBuilder);
    }
}
