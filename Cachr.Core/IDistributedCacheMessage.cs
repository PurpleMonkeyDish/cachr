using Cachr.Core.Messages;

namespace Cachr.Core;

public interface IDistributedCacheMessage
{
    DistributedCacheMessageType Type { get; }
    Guid Id { get; }
    int MaximumWireSize { get; }

    void Encode(BinaryWriter writer);
}
