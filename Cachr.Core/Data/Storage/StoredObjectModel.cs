namespace Cachr.Core.Data.Storage;

public record StoredObjectModel(StoredObjectMetadataModel MetadataModel, byte[]? Data);
