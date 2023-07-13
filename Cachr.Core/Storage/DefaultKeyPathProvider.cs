using System.IO.Hashing;
using System.Text;
using Microsoft.Extensions.Options;

namespace Cachr.Core.Storage;

public class DefaultKeyPathProvider : IKeyPathProvider
{
    private readonly IOptions<FileStorageConfiguration> _options;

    public DefaultKeyPathProvider(IOptions<FileStorageConfiguration> options)
    {
        _options = options;
    }

    public string ComputePath(string key)
    {
        Span<byte> hash = stackalloc byte[8];
        var keyBytes = Encoding.UTF8.GetBytes(key);
        Crc64.Hash(keyBytes, hash);
        var relativePath = Path.Combine(
            _options.Value.Path,
            $"{hash[0]:x2}",
            $"{hash[1]:x2}",
            $"{hash[2]:x2}",
            $"{hash[3]:x2}",
            $"{hash[4]:x2}{hash[5]:x2}{hash[6]:x2}{hash[7]:x2}.data");
        return Path.GetFullPath(relativePath);
    }
}