using System.Text;

namespace Cachr.Core.Protocol;

public sealed record StringCacheValue(string Value) : CacheValue(CacheValueType.String)
{
    protected override int GetValueMaxEncodedSize() => Encoding.UTF8.GetByteCount(Value) + ProtocolConstants.Max7BitEncodedIntBytes;
}