using System.Text;

namespace Cachr.Core.Messages;

public sealed record KeyDeletedDistributedCacheMessage : IDistributedCacheMessage
{
    public KeyDeletedDistributedCacheMessage(BinaryReader reader, Guid id) : this(reader.ReadString())
    {
        Id = id;
    }

    public KeyDeletedDistributedCacheMessage(string key)
    {
        Key = key;
        MaximumWireSize = sizeof(int) + 1 + Encoding.UTF8.GetMaxByteCount(key.Length);
    }

    public string Key { get; }

    public DistributedCacheMessageType Type { get; } = DistributedCacheMessageType.KeyDelete;
    public Guid Id { get; }
    public int MaximumWireSize { get; }

    public void Encode(BinaryWriter writer)
    {
        writer.Write(Key);
    }
}
