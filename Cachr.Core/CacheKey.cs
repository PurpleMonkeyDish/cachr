namespace Cachr.Core;

public record CacheKey
{
    public CacheValue Value { get; }

    public CacheKey(CacheType type, object value)
    {
        Value = new CacheValue(ValidateType(type), ValidateValue(value));
    }

    private static object ValidateValue(object? value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value;
    }

    private static CacheType ValidateType(CacheType type)
    {
        return type switch
        {
            CacheType.String => type,
            CacheType.Integer => type,
            _ => throw new ArgumentException("Cache keys must be either a String, or an integer", nameof(type))
        };
    }

    public CacheKey(CacheValue key)
    {
        ValidateValue(key.Value);
        ValidateType(key.Type);
        Value = key;
    }
}