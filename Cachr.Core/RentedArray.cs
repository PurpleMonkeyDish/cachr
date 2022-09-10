using System.Buffers;

namespace Cachr.Core;

public class RentedArray<T> : IDisposable
{
    private readonly ArrayPool<T>? _pool;
    private readonly ReadOnlyMemory<T> _data;
    private T[]? _dataArray;
    private readonly ArraySegment<T> _segment;

    public static RentedArray<T> FromPool(int minimumSize, ArrayPool<T>? pool)
    {
        return new RentedArray<T>(pool?.Rent(minimumSize) ?? new T[minimumSize], pool, minimumSize);
    }

    public static RentedArray<T> FromDefaultPool(int minimumSize)
    {
        return FromPool(minimumSize, ArrayPool<T>.Shared);
    }

    private RentedArray(T[] data, ArrayPool<T>? pool, int size)
    {
        _data = _segment = new ArraySegment<T>(_dataArray = data, 0, size);
        _pool = pool;
        
    }

    public bool IsPooled
    {
        get
        {
            if (_dataArray is null) throw new ObjectDisposedException(nameof(RentedArray<T>));
            return _pool is not null;
        }
    }

    public ReadOnlyMemory<T> ReadOnlyMemory
    {
        get
        {
            var data = _data;
            if (_dataArray is null) throw new ObjectDisposedException(nameof(RentedArray<T>));
            return data;
        }
    }

    public ArraySegment<T> ArraySegment
    {
        get
        {
            var data = _segment;
            if (_dataArray is null) throw new ObjectDisposedException(nameof(RentedArray<T>));
            return data;
        }
    }


    public static explicit operator ArraySegment<T>(RentedArray<T> rentedArray) => rentedArray.ArraySegment;
    public static explicit operator ReadOnlyMemory<T>(RentedArray<T> rentedArray) => rentedArray.ReadOnlyMemory;


    public void Dispose()
    {
        var data = _dataArray;
        if (IsPooled && data is not null)
        {
            _pool!.Return(data);
        }
        _dataArray = null;
    }
}