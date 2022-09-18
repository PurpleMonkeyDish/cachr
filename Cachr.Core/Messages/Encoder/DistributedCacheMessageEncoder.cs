using System.Text;
using Cachr.Core.Buffers;

namespace Cachr.Core.Messages.Encoder;

public static class DistributedCacheMessageEncoder
{
    public static RentedArray<byte> Encode(IDistributedCacheMessage message)
    {
        using var rented = RentedArray<byte>.FromDefaultPool(message.MaximumWireSize + sizeof(int) + 1 + 16);
        using var memoryStream = new MemoryStream(rented.ArraySegment.Array!, rented.ArraySegment.Offset,
            rented.ArraySegment.Count, true, false);
        memoryStream.Seek(0, SeekOrigin.Begin);
        memoryStream.SetLength(0);

        EncodeTo(memoryStream, message);

        var returnValue = RentedArray<byte>.FromDefaultPool((int)memoryStream.Position);
        rented.ArraySegment.Slice(0, (int)memoryStream.Position).CopyTo(returnValue.ArraySegment);

        return returnValue;
    }

    public static void EncodeTo(Stream stream, IDistributedCacheMessage message)
    {
        using var writer = new BinaryWriter(stream, Encoding.UTF8, true);
        EncodeTo(writer, message);
        writer.Flush();
    }

    public static void EncodeTo(BinaryWriter binaryWriter, IDistributedCacheMessage message)
    {
        binaryWriter.Write7BitEncodedInt((int)message.Type);
        binaryWriter.Write(message.Id.ToByteArray());
        message.Encode(binaryWriter);
    }

    public static IDistributedCacheMessage Decode(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, true);
        return Decode(reader);
    }

    public static IDistributedCacheMessage Decode(BinaryReader reader)
    {
        return DecodeNext(reader);
    }

    private static IDistributedCacheMessage DecodeNext(BinaryReader reader)
    {
        var messageType = (DistributedCacheMessageType)reader.Read7BitEncodedInt();
        var idBytes = reader.ReadBytes(16);
        var id = new Guid(idBytes);
        return Decode(messageType, reader, id);
    }

    public static IDistributedCacheMessage Decode(RentedArray<byte> rented)
    {
        using var memoryStream = new MemoryStream(rented.ArraySegment.Array!, rented.ArraySegment.Offset,
            rented.ArraySegment.Count, true, false);
        memoryStream.Seek(0, SeekOrigin.Begin);
        return Decode(memoryStream);
    }


    private static IDistributedCacheMessage Decode(DistributedCacheMessageType messageType, BinaryReader reader,
        Guid id)
    {
        return messageType switch
        {
            DistributedCacheMessageType.GetKeys => GetKeysDistributedCacheMessage.Instance,
            DistributedCacheMessageType.GetKeyResponse => new GetKeysResponseDistributedCacheMessage(reader, id),
            DistributedCacheMessageType.GetKeyData => new GetKeyDataDistributedCacheMessage(reader, id),
            DistributedCacheMessageType.GetKeyDataResponse => new GetKeyDataResponseDistributedCacheMessage(reader, id),
            DistributedCacheMessageType.KeySet => new KeySetDistributedCacheMessage(reader, id),
            DistributedCacheMessageType.KeyDelete => new KeyDeletedDistributedCacheMessage(reader, id),
            _ => throw new NotImplementedException()
        };
    }
}
