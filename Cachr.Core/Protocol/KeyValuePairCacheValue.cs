namespace Cachr.Core.Protocol;

public sealed record KeyValuePairCacheValue : CacheValue
{
    public KeyValuePairCacheValue(CacheValue key, CacheValue value) : base(CacheValueType.KeyValuePair)
    {
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (key.ValueType)
        {
            case CacheValueType.String:
            case CacheValueType.Integer:
            case CacheValueType.UnsignedInteger:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(key), "Cache key must be an integer, unsigned integer, or string.");
        }

        Key = key;
        Value = value;
    }

    public CacheValue Value { get; init; }

    public CacheValue Key { get; init; }

    protected override int GetValueMaxEncodedSize() => Key.GetEstimatedSize() + Value.GetEstimatedSize();
}