namespace Cachr.Core.Storage;

public record StoredObject(StoredObjectMetadata Metadata, byte[]? Data);