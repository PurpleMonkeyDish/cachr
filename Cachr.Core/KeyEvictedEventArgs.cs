using Microsoft.Extensions.Caching.Memory;

namespace Cachr.Core;

public class KeyEvictedEventArgs : EventArgs
{
    public string Key { get; init; }
    public EvictionReason EvictionReason { get; init; }
}