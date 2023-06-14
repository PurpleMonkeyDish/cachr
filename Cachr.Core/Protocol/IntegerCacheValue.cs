namespace Cachr.Core.Protocol;

public sealed record IntegerCacheValue(long Value) : CacheValue(CacheValueType.Integer)
{
    protected override int GetValueMaxEncodedSize() => ProtocolConstants.Max7BitEncodedIntBytes;
}