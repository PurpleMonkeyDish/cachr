using System.Buffers;
using System.Text;

namespace Cachr.Core.Protocol;

public static class ProtocolSerializerExtensions {
    public static async Task<byte[]> GetBytesAsync<T>(this IProtocolSerializer<T> serializer,
        T value,
        CancellationToken cancellationToken = default)
    {
        var bufferSize = serializer.GetDesiredBufferSize(value);
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        using var memoryStream = new MemoryStream(buffer, true);
        memoryStream.Seek(0, SeekOrigin.Begin);
        await using var binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8, true);
        await serializer.WriteAsync(binaryWriter, value, cancellationToken).ConfigureAwait(false);
        binaryWriter.Flush();
        await memoryStream.FlushAsync(cancellationToken);
        return memoryStream.ToArray();
    }

    public static async Task<T> ReadBytesAsync<T>(this IProtocolSerializer<T> serializer, byte[] data, CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream(data, false);
        memoryStream.Seek(0, SeekOrigin.Begin);
        using var binaryReader = new BinaryReader(memoryStream, Encoding.UTF8, true);
        return await serializer.ReadAsync(binaryReader, cancellationToken);
    }
}