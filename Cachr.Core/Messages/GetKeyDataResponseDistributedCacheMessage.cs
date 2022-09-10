using System.Text;

namespace Cachr.Core.Messages;

public record GetKeyDataResponseDistributedCacheMessage : IDistributedCacheMessage
{
    public GetKeyDataResponseDistributedCacheMessage(string key, byte[] data)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(data);
        Key = key;
        Data = data;
        MaximumWireSize = sizeof(int) + 1 + Encoding.UTF8.GetMaxByteCount(Key.Length) + sizeof(int) + 1 + Data.Length;
    }
    public GetKeyDataResponseDistributedCacheMessage(BinaryReader reader, Guid id) 
        : this(reader.ReadString(), reader.ReadBytes(reader.Read7BitEncodedInt()))
    {
        Id = id;
    }
    public DistributedCacheMessageType Type { get; } = DistributedCacheMessageType.GetKeyDataResponse;
    public Guid Id { get; }
    public string Key { get; }
    public byte[] Data { get; }
    public int MaximumWireSize { get; }
    public void Encode(BinaryWriter writer)
    {
        writer.Write(Key);
        writer.Write7BitEncodedInt(Data.Length);
        writer.Write(Data);
    }
}