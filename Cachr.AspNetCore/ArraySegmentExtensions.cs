using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using Cachr.Core.Buffers;

namespace Cachr.AspNetCore;

internal static class ArraySegmentExtensions
{
    internal static readonly byte[] DeflateSentinel = new byte[] {0, 0};
    internal static readonly byte[] BrotliSentinel = new byte[] {0, 1};
    internal static readonly byte[] GzipSentinel = new byte[] {0, 2};
    internal static readonly byte[] NoneSentinel = new byte[] {0, 3};

    private static (byte[] Sentinel, Func<Stream, Stream> StreamWrapperFactory)[] _compressionMethods =
        new (byte[], Func<Stream, Stream>)[]
        {
            (GzipSentinel, s => new GZipStream(s, CompressionMode.Decompress, false)),
            (BrotliSentinel, s => new BrotliStream(s, CompressionMode.Decompress, false)),
            (DeflateSentinel, s => new DeflateStream(s, CompressionMode.Decompress, false)), (NoneSentinel, s => s),
            (NoneSentinel, s => s)
        };

    internal static MemoryStream ToMemoryStream(this RentedArray<byte> rentedArray, bool writable = true) =>
        rentedArray.ArraySegment.ToMemoryStream(writable);

    internal static MemoryStream ToMemoryStream(this ArraySegment<byte> segment, bool writable = true)
    {
        var memoryStream = new MemoryStream(segment.Array ?? Array.Empty<byte>(), segment.Offset, segment.Count);
        if (memoryStream.Position != 0)
            memoryStream.Seek(0, SeekOrigin.Begin);
        return memoryStream;
    }


    internal static Stream DetectAndWrapWithDecompressionStream(this RentedArray<byte> rentedArray) =>
        rentedArray.ArraySegment.DetectAndWrapWithDecompressionStream();

    internal static Stream DetectAndWrapWithDecompressionStream(this ArraySegment<byte> arraySegment)
    {
        foreach (var compressionMethod in _compressionMethods)
        {
            if (TryCreateDecompressionStream(arraySegment, compressionMethod.Sentinel,
                    compressionMethod.StreamWrapperFactory,
                    out var stream))
                return stream;
        }

        throw new InvalidOperationException("Could not determine compression method");
    }


    internal static bool TryCreateDecompressionStream(this ArraySegment<byte> arraySegment, byte[] sentinel,
        Func<Stream, Stream> compressionStreamFactory, [NotNullWhen(true)] out Stream? stream)
    {
        stream = null;
        if (arraySegment.Count == 0)
        {
            stream = ArraySegment<byte>.Empty.ToMemoryStream();
            return true;
        }

        if (arraySegment.Count < sentinel.Length) return false;

        if (!arraySegment[..sentinel.Length].SequenceEqual(sentinel)) return false;
        if (arraySegment.Count == sentinel.Length)
        {
            stream = ArraySegment<byte>.Empty.ToMemoryStream();
            return true;
        }

        var memoryStream = arraySegment[sentinel.Length..].ToMemoryStream(false);
        stream = compressionStreamFactory(memoryStream);

        return true;
    }
}