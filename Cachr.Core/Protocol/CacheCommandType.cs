namespace Cachr.Core.Protocol;

public enum CacheCommandType
{
    Invalid,
    Get,
    Set,
    SetExpiration,
    Remove,
    CompareExchange,
    Increment,
    Decrement,
    Ping
}