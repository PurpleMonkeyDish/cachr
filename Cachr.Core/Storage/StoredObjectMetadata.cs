using System;

namespace Cachr.Core.Storage;

public record StoredObjectMetadata(string Path, DateTimeOffset LastModified);
