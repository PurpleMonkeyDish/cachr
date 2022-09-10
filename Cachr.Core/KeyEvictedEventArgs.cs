using Microsoft.Extensions.Caching.Memory;

namespace Cachr.Core;

public class KeyEvictedEventArgs : EventArgs
{
    public KeyEvictedEventArgs(string key, EvictionReason evictionReason)
    {
        Key = key;
        EvictionReason = evictionReason;
    }
    public string Key { get; }
    public EvictionReason EvictionReason { get; }
}