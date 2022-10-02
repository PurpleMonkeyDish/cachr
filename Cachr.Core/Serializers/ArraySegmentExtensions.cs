using System.IO.Compression;
using Cachr.Core.Buffers;

namespace Cachr.Core.Serializers;

internal static class ArraySegmentExtensions
{
    internal static readonly byte[] GzipSentinel = new byte[] {3};
    internal static readonly byte[] NoneSentinel = new byte[] {0};

    private static (byte[] Sentinel, Func<Stream, Stream> DecompressionStreamWrapperFactory, Func<Stream, Stream>
        CompressionFactory)[] _compressionMethods =
            new (byte[], Func<Stream, Stream>, Func<Stream, Stream>)[]
            {
                (GzipSentinel, static s => new GZipStream(s, CompressionMode.Decompress, false),
                    static s => new GZipStream(s, CompressionLevel.Optimal, true)),
                (NoneSentinel, static s => s, static s => s),
            };

    internal static MemoryStream ToMemoryStream(this RentedArray<byte> rentedArray, bool writable = true) =>
        rentedArray.ArraySegment.ToMemoryStream(writable);

    internal static MemoryStream ToMemoryStream(this ArraySegment<byte> segment, bool writable = true)
    {
        var memoryStream =
            new MemoryStream(segment.Array ?? Array.Empty<byte>(), segment.Offset, segment.Count, writable);
        memoryStream.Seek(0, SeekOrigin.Begin);
        return memoryStream;
    }
}
