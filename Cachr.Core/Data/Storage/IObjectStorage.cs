namespace Cachr.Core.Data.Storage;

public interface IObjectStorage
{
    StoredObjectMetadataModel? GetMetadata(string key);
    Task<StoredObjectModel?> GetDataAsync(string key, CancellationToken cancellationToken);
    Task CreateOrUpdateAsync(string key, byte[] data, CancellationToken cancellationToken);
}

