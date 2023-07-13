namespace Cachr.Core.Storage;

public interface IObjectStorage
{
    StoredObjectMetadata? GetMetadata(string key);
    Task<StoredObject?> GetDataAsync(string key, CancellationToken cancellationToken);
    Task CreateOrUpdateAsync(string key, byte[] data, CancellationToken cancellationToken);
}