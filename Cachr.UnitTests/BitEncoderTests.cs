using Cachr.Core;
using Cachr.Core.Protocol;
using FsCheck.Xunit;

namespace Cachr.UnitTests;

public sealed class BitEncoderTests
{
    [Fact]
    public void CanDecodeEncodedValue()
    {
        const ulong expected = 1234;
        const bool expectedReturnValue = true;
        var expectedBytes = new byte[]
        {
            210,
            9
        };
        const int expectedBytesWritten = 2;
        Span<byte> bytes = stackalloc byte[2];
        var bytesWritten = BitEncoder.Encode7Bit(bytes, expected);
        Assert.Equal(expectedBytesWritten, bytesWritten);
        Assert.Equal(expectedBytes, bytes.ToArray());
        var actualReturnValue = BitEncoder.TryDecode7BitUInt64(bytes, out var actual, out var bytesConsumed);
        Assert.Equal(expectedReturnValue, actualReturnValue);
        Assert.Equal(expectedBytesWritten, bytesConsumed);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    public void MaximumSignedBytesTest(long value)
    {
        Span<byte> bytes = stackalloc byte[BitEncoder.MaximumBytes];
        BitEncoder.Encode7Bit(bytes, value);
        Assert.True(BitEncoder.TryDecode7BitInt64(bytes, out var v, out var bytesRead));
        Assert.Equal(value, v);
        Assert.Equal(BitEncoder.MaximumBytes, bytesRead);
    }

    [Fact]
    public void CanEncodeAndDecodeZero()
    {
        const ulong expected = 0;
        const int expectedLength = 1;
        Span<byte> bytes = stackalloc byte[1];
        bytes[0] = 63; // Catch if it doesn't write 0, must be less than 64
        var bytesWritten = BitEncoder.Encode7Bit(bytes, expected);
        Assert.Equal(expectedLength, bytesWritten);
        Assert.True(BitEncoder.TryDecode7BitUInt64(bytes, out var actual, out var bytesRead));
        Assert.Equal(expectedLength, bytesRead);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ReturnsFalseWhenDecodingInvalidData()
    {
        Span<byte> bytes = stackalloc byte[BitEncoder.MaximumBytes + 1];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = 0x80;
        }
        Assert.False(BitEncoder.TryDecode7BitUInt64(bytes, out _, out _));
    }

    [Fact]
    public void DecodeReturnsFalseWhenPassedEmptySpan()
    {
        Span<byte> bytes = stackalloc byte[0];
        Assert.False(BitEncoder.TryDecode7BitUInt64(bytes, out _, out _));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(500)]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    [InlineData(-1)]
    [InlineData(-500)]
    public void GenericTest(long value)
    {
        Span<byte> bytes = stackalloc byte[BitEncoder.MaximumBytes];
        BitEncoder.Encode7Bit(bytes, value);
        Assert.True(BitEncoder.TryDecode7BitInt64(bytes, out var v, out _));
        var signedActual = unchecked((long)v);
        Assert.Equal(value, signedActual);
    }

    [Property(MaxTest = 5000)]
    public void Int64PropertyTest(long value)
    {
        Span<byte> bytes = stackalloc byte[BitEncoder.MaximumBytes];
        BitEncoder.Encode7Bit(bytes, value);
        Assert.True(BitEncoder.TryDecode7BitInt64(bytes, out var v, out _));
        Assert.Equal(value, v);
    }
}
