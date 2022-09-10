namespace Cachr.Core.Messages;

public enum DistributedCacheMessageType
{
    NoOperation,
    GetKeys,
    GetKeyResponse,
    GetKeyData,
    GetKeyDataResponse,
    KeySet,
    KeyDelete
}