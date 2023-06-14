using System.Collections.Immutable;

namespace Cachr.Core.Protocol;

public sealed record MapCacheValue : CacheValue
{
    public MapCacheValue(IEnumerable<KeyValuePairCacheValue> values)
        : base(CacheValueType.Map)
    {
        Value = values.ToImmutableDictionary(i => i.Key, i => i.Value);
    }

    public ImmutableDictionary<CacheValue, CacheValue> Value { get; init; }
    protected override int GetValueMaxEncodedSize() => ProtocolConstants.Max7BitEncodedIntBytes + Value.Sum(i => i.Key.GetEstimatedSize() + i.Value.GetEstimatedSize());
}