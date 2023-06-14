namespace Cachr.Core.Protocol;

public abstract record CacheValue(CacheValueType ValueType)
{
    internal int GetEstimatedSize() => ProtocolConstants.Max7BitEncodedIntBytes + GetValueMaxEncodedSize();
    protected abstract int GetValueMaxEncodedSize();
}