using System.Buffers;
using System.Runtime.CompilerServices;

namespace Cachr.Core.Protocol;

internal static class BitEncoder
{
    public const int MaximumBytes = 10;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Encode7Bit(Span<byte> bytes, long value)
    {
        return Encode7Bit(bytes, ZigZagEncode(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Encode7Bit(Span<byte> bytes, ulong value)
    {
        unchecked
        {
            var index = 0;
            for (; index < MaximumBytes; index++)
            {
                if (value == 0) break;
                if (index >= bytes.Length) throw new InvalidOperationException("Buffer is too small.");
                var currentByte = (byte)(value & 0x7F);
                // ReSharper disable once ConvertToCompoundAssignment
                value = value >> 7;
                if (value != 0)
                {
                    currentByte = (byte)(currentByte | 0x80);
                }

                bytes[index] = currentByte;
            }

            if (index == 0)
            {
                bytes[index] = 0;
                index++;
            }

            return index;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryDecode7BitInt64(ReadOnlySpan<byte> bytes, out long value, out int bytesConsumed)
    {
        value = default;
        bytesConsumed = default;
        if (!TryDecode7BitUInt64(bytes, out var unsignedValue, out bytesConsumed)) return false;
        value = ZigZagDecode(unsignedValue);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong ZigZagEncode(long value) => unchecked((ulong)unchecked((value >> 63) ^ (value << 1)));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long ZigZagDecode(ulong value) => (unchecked((long)value) >>> 1) ^ -(unchecked((long)value) & 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async ValueTask<long> DecodeInt64FromStreamAsync(Stream stream, CancellationToken cancellationToken)
    {
        return ZigZagDecode(await DecodeUInt64FromStreamAsync(stream, cancellationToken));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async ValueTask<ulong> DecodeUInt64FromStreamAsync(Stream stream, CancellationToken cancellationToken)
    {
        ulong value = default;
        var buffer = ArrayPool<byte>.Shared.Rent(1024);
        try
        {
            for (var index = 0; index < MaximumBytes; index++)
            {
                await stream.ReadExactlyAsync(buffer.AsMemory(0, 1), cancellationToken);
                value = value | ((ulong)(buffer[0] & 0x7Fu) << (index * 7));
                if ((buffer[0] & 0x80) == 0) return value;
            }

            throw new InvalidOperationException("Unable to decode 7 bit encoded integer, the data is too long.");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryDecode7BitUInt64(ReadOnlySpan<byte> bytes, out ulong value, out int bytesConsumed)
    {
        value = default;
        bytesConsumed = default;

        if (bytes.Length == 0) return false;
        byte last = 0x80;
        int index;
        for (index = 0; index < bytes.Length && (last & 0x80) != 0; index++)
        {
            value = value | ((ulong)((last = bytes[index]) & 0x7Fu) << (index * 7));
        }

        bytesConsumed = index;
        return (last & 0x80) == 0;
    }
}
