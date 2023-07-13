using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace Cachr.Core.Storage;

public class FileObjectStorage : IObjectStorage
{
    private readonly IKeyPathProvider _pathProvider;
    private readonly IOptions<FileStorageConfiguration> _options;

    public FileObjectStorage(IKeyPathProvider pathProvider, IOptions<FileStorageConfiguration> options)
    {
        _pathProvider = pathProvider;
        _options = options;
    }

    public StoredObjectMetadata? GetMetadata(string key)
    {
        var fullPath = _pathProvider.ComputePath(key);
        var directoryPath = Path.GetDirectoryName(fullPath);

        if (!Directory.Exists(directoryPath)) return null;
        if (!File.Exists(fullPath)) return null;
        var modified = new DateTimeOffset(File.GetLastWriteTimeUtc(fullPath));
        return new StoredObjectMetadata(fullPath, modified);
    }

    public async Task<StoredObject?> GetDataAsync(string key, CancellationToken cancellationToken)
    {
        var metadata = GetMetadata(key);
        if (metadata is null) return null;
        var data = await File.ReadAllBytesAsync(metadata.Path, cancellationToken);

        return new StoredObject(metadata, data);
    }

    public async Task CreateOrUpdateAsync(string key, byte[] data, CancellationToken cancellationToken)
    {
        var targetPath = _pathProvider.ComputePath(key);
        var directoryPart = Path.GetDirectoryName(targetPath);
        Debug.Assert(directoryPart != null, "directoryPart != null");
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            Directory.CreateDirectory(directoryPart);
        }
        else
        {
            // This is wrapped in an if block specifically to avoid windows.
#pragma warning disable CA1416
            Directory.CreateDirectory(directoryPart,
                UnixFileMode.UserWrite | UnixFileMode.UserRead | UnixFileMode.GroupRead);
#pragma warning restore CA1416
        }

        var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        await File.WriteAllBytesAsync(tempFile, data, cancellationToken);
        File.Move(tempFile, _pathProvider.ComputePath(key), overwrite: true);
    }
}