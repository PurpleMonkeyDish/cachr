using System.Text;

namespace Cachr.Core.Messages;

public record GetKeysResponseDistributedCacheMessage : IDistributedCacheMessage
{
    public GetKeysResponseDistributedCacheMessage(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        Key = key;
    }
    public GetKeysResponseDistributedCacheMessage(BinaryReader reader, Guid id) 
        : this(reader.ReadString())
    {
        Id = id;
    }
    public DistributedCacheMessageType Type { get; } = DistributedCacheMessageType.GetKeyResponse;
    public Guid Id { get; } = Guid.NewGuid();
    public string Key { get; init; }
    public int MaximumWireSize => sizeof(int) + 1 + Encoding.UTF8.GetMaxByteCount(Key.Length);
    public void Encode(BinaryWriter writer)
    {
        writer.Write(Key);
    }
}