using System.Collections.Immutable;

namespace Cachr.Core.Protocol;

public sealed record BytesCacheValue(ImmutableArray<byte> Value) : CacheValue(CacheValueType.Bytes)
{
    protected override int GetValueMaxEncodedSize()
    {
        return ProtocolConstants.Max7BitEncodedIntBytes + Value.Length;
    }
}