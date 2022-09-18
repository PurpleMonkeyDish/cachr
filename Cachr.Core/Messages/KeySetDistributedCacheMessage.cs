using System.Text;

namespace Cachr.Core.Messages;

public sealed record KeySetDistributedCacheMessage : IDistributedCacheMessage
{
    public KeySetDistributedCacheMessage(string key, byte[] data, int slidingTimeToLiveMilliseconds,
        long expirationTimeStampUnixMilliseconds)
    {
        Key = key;
        Data = data;
        SlidingTimeToLiveMilliseconds = slidingTimeToLiveMilliseconds;
        ExpirationTimeStampUnixMilliseconds = expirationTimeStampUnixMilliseconds;
        MaximumWireSize =
            sizeof(int) + 1 +
            Encoding.UTF8.GetMaxByteCount(Key.Length) +
            sizeof(int) + 1 +
            Data.Length +
            sizeof(int) + 1 +
            sizeof(long) + 1;
    }

    public KeySetDistributedCacheMessage(BinaryReader reader, Guid id)
        : this
        (
            reader.ReadString(),
            reader.ReadBytes
            (
                reader.Read7BitEncodedInt()
            ),
            reader.Read7BitEncodedInt(),
            reader.Read7BitEncodedInt64()
        )
    {
        Id = id;
    }

    public string Key { get; }
    public byte[] Data { get; }
    public int SlidingTimeToLiveMilliseconds { get; }
    public long ExpirationTimeStampUnixMilliseconds { get; }
    public DistributedCacheMessageType Type { get; } = DistributedCacheMessageType.KeySet;
    public Guid Id { get; } = Guid.NewGuid();
    public int MaximumWireSize { get; }


    public void Encode(BinaryWriter writer)
    {
        writer.Write(Key);
        writer.Write7BitEncodedInt(Data.Length);
        writer.Write(Data);
        writer.Write7BitEncodedInt(SlidingTimeToLiveMilliseconds);
        writer.Write7BitEncodedInt64(ExpirationTimeStampUnixMilliseconds);
    }
}
