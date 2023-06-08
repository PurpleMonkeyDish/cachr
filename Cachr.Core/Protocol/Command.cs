namespace Cachr.Core.Protocol;

public enum Command : byte
{
    Set, // (Key, Value)+
    Get, // Key+
    Exists, // Key+
    Increment, // Key+
    SetAdd, // Key, Value+
    SetRemove, // Key, Value+
    HashSetAdd, // Key (HashKey, HashValue)+
    HashSetRemove, // Key, HashKey+
    Remove, // Key+
    Clear, // None
    SetExpiration, // Key ((Type:Absolute|Sliding) Value){1,2} unique
    Batch, // Command Payload 1...n
}