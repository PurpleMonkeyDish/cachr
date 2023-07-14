namespace Cachr.Core.Data;

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
