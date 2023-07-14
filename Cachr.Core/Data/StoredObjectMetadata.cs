namespace Cachr.Core.Data;

public class StoredObjectMetadata
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public long? AbsoluteExpiration { get; set; }
    public double? SlidingExpiration { get; set; }
    public long Created { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    public long Modified { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    public long LastAccess { get; set; } = DateTimeOffset.Now.ToUnixTimeMilliseconds();
}
