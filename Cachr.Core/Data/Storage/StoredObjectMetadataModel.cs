namespace Cachr.Core.Data.Storage;

public record StoredObjectMetadataModel(string Path, DateTimeOffset LastModified, DateTimeOffset? AbsoluteExpiration = default, DateTimeOffset? SlidingExpiration = default);
