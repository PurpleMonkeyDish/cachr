namespace Cachr.Core;

public record CacheEntry(CacheKey Key,
    CacheValue Value,
    long? AbsoluteExpirationMilliseconds,
    long? SlidingExpirationMilliseconds);