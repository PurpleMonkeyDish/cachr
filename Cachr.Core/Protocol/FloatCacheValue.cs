namespace Cachr.Core.Protocol;

public sealed record FloatCacheValue(double Value) : CacheValue(CacheValueType.Float)
{
    protected override int GetValueMaxEncodedSize() => 8;
}