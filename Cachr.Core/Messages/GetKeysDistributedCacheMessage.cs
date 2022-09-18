namespace Cachr.Core.Messages;

public sealed record class GetKeysDistributedCacheMessage : IDistributedCacheMessage
{
    private GetKeysDistributedCacheMessage() { }

    public static GetKeysDistributedCacheMessage Instance { get; } = new();
    public DistributedCacheMessageType Type { get; } = DistributedCacheMessageType.GetKeys;
    public int MaximumWireSize { get; } = 0;
    public Guid Id => Guid.Empty;

    public void Encode(BinaryWriter writer)
    {
    }
}
