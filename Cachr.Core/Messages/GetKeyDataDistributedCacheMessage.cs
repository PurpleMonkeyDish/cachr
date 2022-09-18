using System.Text;

namespace Cachr.Core.Messages;

public sealed record GetKeyDataDistributedCacheMessage : IDistributedCacheMessage
{
    public GetKeyDataDistributedCacheMessage(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        Key = key;
        MaximumWireSize = sizeof(int) + 1 + Encoding.UTF8.GetMaxByteCount(Key.Length);
    }

    public GetKeyDataDistributedCacheMessage(BinaryReader reader, Guid id)
        : this(reader.ReadString())
    {
        Id = id;
    }

    public string Key { get; }
    public DistributedCacheMessageType Type { get; } = DistributedCacheMessageType.GetKeyData;
    public Guid Id { get; } = Guid.NewGuid();
    public int MaximumWireSize { get; }

    public void Encode(BinaryWriter writer)
    {
        writer.Write(Key);
    }
}
