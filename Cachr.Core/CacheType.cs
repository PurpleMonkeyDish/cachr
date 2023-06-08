namespace Cachr.Core;

public enum CacheType : byte
{
    String,
    Bytes,
    Integer,
    SignedInteger,
    Float,
    List,
    HashMap,
    HashSet,
    CommandPayload
}