using System.Collections.Immutable;

namespace Cachr.Core.Protocol;

public sealed record SetCacheValue(ImmutableArray<CacheValue> Value) : CacheValue(CacheValueType.Set)
{
    protected override int GetValueMaxEncodedSize()
    {
        return ProtocolConstants.Max7BitEncodedIntBytes + Value.Select(i => i.GetEstimatedSize()).Sum();
    }
}