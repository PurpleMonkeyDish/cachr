using System;
using Cachr.Core.Buffers;
using Xunit;

namespace Cachr.UnitTests;

public sealed class RentedArrayTests
{
    [Fact]
    public void RentedArrayCanBeCreatedWithNullPool()
    {
        const int ExpectedSize = 10;
        using var rented = RentedArray<byte>.FromPool(ExpectedSize, null);
        for (var x = 0; x < ExpectedSize; x++)
        {
            rented.ArraySegment.Array![x] = (byte)x;
        }

        Assert.False(rented.IsPooled);
        Assert.Equal(ExpectedSize, rented.ReadOnlyMemory.Length);
        Assert.Equal(ExpectedSize, rented.ArraySegment.Count);

        Assert.Equal(rented.ArraySegment, rented.ReadOnlyMemory);
    }

    [Fact]
    public void RentedArrayRentsFromPoolCorrectly()
    {
        const int ExpectedSize = 10;
        using var rented = RentedArray<byte>.FromDefaultPool(ExpectedSize);
        Assert.True(rented.IsPooled);
        Assert.Equal(ExpectedSize, rented.ArraySegment.Count);
        Assert.InRange(rented.ArraySegment.Array!.Length, ExpectedSize, int.MaxValue);
        Assert.Equal(ExpectedSize, rented.ReadOnlyMemory.Length);
    }

    [Fact]
    public void PropertiesThrowObjectDisposedExceptionWhenDisposed()
    {
        const int ExpectedSize = 10;
        var rented = RentedArray<byte>.FromDefaultPool(ExpectedSize);
        rented.Dispose();

        Assert.Throws<ObjectDisposedException>(() => rented.ArraySegment);
        Assert.Throws<ObjectDisposedException>(() => rented.ReadOnlyMemory);
        Assert.Throws<ObjectDisposedException>(() => rented.IsPooled);
    }

    [Fact]
    public void RentedArrayCanBeCastToReadOnlyMemory()
    {
        const int ExpectedSize = 10;
        using var rented = RentedArray<byte>.FromDefaultPool(ExpectedSize);
        var readOnlyMemory = (ReadOnlyMemory<byte>)rented;
        Assert.Equal(readOnlyMemory, rented.ReadOnlyMemory);
    }

    [Fact]
    public void RentedArrayCanBeCastToArraySegment()
    {
        const int ExpectedSize = 10;
        using var rented = RentedArray<byte>.FromDefaultPool(ExpectedSize);
        var readOnlyMemory = (ArraySegment<byte>)rented;
        Assert.Equal(readOnlyMemory, rented.ArraySegment);
    }

    [Fact]
    public void CastOperatorsThrowObjectDisposedExceptionWhenDisposed()
    {
        const int ExpectedSize = 10;
        var rented = RentedArray<byte>.FromDefaultPool(ExpectedSize);
        rented.Dispose();

        Assert.Throws<ObjectDisposedException>(() => (ReadOnlyMemory<byte>)rented);
        Assert.Throws<ObjectDisposedException>(() => (ArraySegment<byte>)rented);
    }
}
