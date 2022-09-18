using System;
using Cachr.Core.Buffers;
using Xunit;

namespace Cachr.UnitTests;

public sealed class RentedArrayTests
{
    [Fact]
    public void RentedArrayCanBeCreatedWithNullPool()
    {
        const int expectedSize = 10;
        using var rented = RentedArray<byte>.FromPool(expectedSize, null);
        for (var x = 0; x < expectedSize; x++)
        {
            rented.ArraySegment.Array![x] = (byte)x;
        }

        Assert.False(rented.IsPooled);
        Assert.Equal(expectedSize, rented.ReadOnlyMemory.Length);
        Assert.Equal(expectedSize, rented.ArraySegment.Count);

        Assert.Equal(rented.ArraySegment, rented.ReadOnlyMemory);
    }

    [Fact]
    public void RentedArrayRentsFromPoolCorrectly()
    {
        const int expectedSize = 10;
        using var rented = RentedArray<byte>.FromDefaultPool(expectedSize);
        Assert.True(rented.IsPooled);
        Assert.Equal(expectedSize, rented.ArraySegment.Count);
        Assert.InRange(rented.ArraySegment.Array!.Length, expectedSize, int.MaxValue);
        Assert.Equal(expectedSize, rented.ReadOnlyMemory.Length);
    }

    [Fact]
    public void PropertiesThrowObjectDisposedExceptionWhenDisposed()
    {
        const int expectedSize = 10;
        var rented = RentedArray<byte>.FromDefaultPool(expectedSize);
        rented.Dispose();

        Assert.Throws<ObjectDisposedException>(() => rented.ArraySegment);
        Assert.Throws<ObjectDisposedException>(() => rented.ReadOnlyMemory);
        Assert.Throws<ObjectDisposedException>(() => rented.IsPooled);
    }

    [Fact]
    public void RentedArrayCanBeCastToReadOnlyMemory()
    {
        const int expectedSize = 10;
        using var rented = RentedArray<byte>.FromDefaultPool(expectedSize);
        var readOnlyMemory = (ReadOnlyMemory<byte>)rented;
        Assert.Equal(readOnlyMemory, rented.ReadOnlyMemory);
    }

    [Fact]
    public void RentedArrayCanBeCastToArraySegment()
    {
        const int expectedSize = 10;
        using var rented = RentedArray<byte>.FromDefaultPool(expectedSize);
        var readOnlyMemory = (ArraySegment<byte>)rented;
        Assert.Equal(readOnlyMemory, rented.ArraySegment);
    }

    [Fact]
    public void CastOperatorsThrowObjectDisposedExceptionWhenDisposed()
    {
        const int expectedSize = 10;
        var rented = RentedArray<byte>.FromDefaultPool(expectedSize);
        rented.Dispose();

        Assert.Throws<ObjectDisposedException>(() => (ReadOnlyMemory<byte>)rented);
        Assert.Throws<ObjectDisposedException>(() => (ArraySegment<byte>)rented);
    }
}
