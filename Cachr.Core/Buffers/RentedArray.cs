using System.Buffers;
using System.Text.Json.Serialization;

namespace Cachr.Core.Buffers;

[JsonConverter(typeof(RentedArrayJsonConverterFactory))]
public sealed class RentedArray<T> : IDisposable
{
    private readonly ReadOnlyMemory<T> _data;
    private readonly ArrayPool<T>? _pool;
    private readonly ArraySegment<T> _segment;
    private T[]? _dataArray;
    private readonly bool _disposable;

    private RentedArray(T[] data, ArrayPool<T>? pool, int size, bool disposable = true)
    {
        if (size == 0)
        {
            _segment = ArraySegment<T>.Empty;
            _pool = null;
            _data = _dataArray = Array.Empty<T>();
        }
        else
        {
            _data = _segment = new ArraySegment<T>(_dataArray = data, 0, size);
            _pool = pool;
        }

        _disposable = disposable;
    }

    public bool IsPooled
    {
        get
        {
            if (_dataArray is null)
            {
                throw new ObjectDisposedException(nameof(RentedArray<T>));
            }

            return _pool is not null;
        }
    }

    public ReadOnlyMemory<T> ReadOnlyMemory
    {
        get
        {
            var data = _data;
            if (_dataArray is null)
            {
                throw new ObjectDisposedException(nameof(RentedArray<T>));
            }

            return data;
        }
    }

    public ArraySegment<T> ArraySegment
    {
        get
        {
            var data = _segment;
            if (_dataArray is null)
            {
                throw new ObjectDisposedException(nameof(RentedArray<T>));
            }

            return data;
        }
    }

    public static RentedArray<T> Empty { get; } = new RentedArray<T>(Array.Empty<T>(), null, 0, false);


    public void Dispose()
    {
        if (!_disposable) return;
        var data = _dataArray;
        if (IsPooled && data is not null)
        {
            _pool!.Return(data);
        }

        _dataArray = null;
    }

    public static RentedArray<T> FromPool(int minimumSize, ArrayPool<T>? pool)
    {
        return new RentedArray<T>(pool?.Rent(minimumSize) ?? new T[minimumSize], pool, minimumSize);
    }

    public static RentedArray<T> FromDefaultPool(int minimumSize)
    {
        return FromPool(minimumSize, ArrayPool<T>.Shared);
    }


    public static explicit operator ArraySegment<T>(RentedArray<T> rentedArray)
    {
        return rentedArray.ArraySegment;
    }

    public static explicit operator ReadOnlyMemory<T>(RentedArray<T> rentedArray)
    {
        return rentedArray.ReadOnlyMemory;
    }

    public override string ToString()
    {
        return $"RentedArray: Pooled: {IsPooled}, Length: {ArraySegment.Count}, {string.Join(", ", ArraySegment)}";
    }
}
