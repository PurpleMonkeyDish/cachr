using System.IO.Hashing;
using System.Text;

namespace Cachr.Core.Cache;

public class ShardSelector : IShardSelector
{
    public int ShardCount => 8192;

    public int SelectShard(string key)
    {
        Span<byte> crc = stackalloc byte[4];
        var inputByteCount = Encoding.UTF8.GetByteCount(key);
        Span<byte> inputBytes = stackalloc byte[inputByteCount];
        Crc32.Hash(inputBytes, crc);
        if (!BitConverter.IsLittleEndian)
        {
            crc.Reverse();
        }

        var result = BitConverter.ToInt32(crc);
        if (result < 0)
        {
            result = ~result;
        }

        return result % ShardCount;
    }
}
