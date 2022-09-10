namespace Cachr.Core.Messages;

public record class GetKeysDistributedCacheMessage : IDistributedCacheMessage
{
    private GetKeysDistributedCacheMessage() { }
    public DistributedCacheMessageType Type { get; } = DistributedCacheMessageType.GetKeys;
    public int MaximumWireSize { get; } = 0;
    public Guid Id => Guid.Empty;
    public void Encode(BinaryWriter writer)
    {
    }

    public static GetKeysDistributedCacheMessage Instance { get; } = new GetKeysDistributedCacheMessage();
}