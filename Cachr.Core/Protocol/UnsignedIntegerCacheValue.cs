namespace Cachr.Core.Protocol;

public sealed record UnsignedIntegerCacheValue(ulong Value) : CacheValue(CacheValueType.UnsignedInteger)
{
    protected override int GetValueMaxEncodedSize() => ProtocolConstants.Max7BitEncodedIntBytes;
}