namespace Cachr.Core.Protocol;

public sealed record NullCacheValue : CacheValue
{
    private NullCacheValue()
        : base(CacheValueType.Null)
    {

    }
    public static NullCacheValue Instance = new();
    protected override int GetValueMaxEncodedSize() => 0;
}