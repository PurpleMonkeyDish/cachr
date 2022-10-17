using System.Buffers;
using System.Text.Json.Serialization;

namespace Cachr.Core.Buffers;

[JsonConverter(typeof(RentedArrayJsonConverterFactory))]
public sealed class RentedArray<T> : IDisposable
{
    private ReadOnlyMemory<T> _data;
    private ArrayPool<T>? _pool;
    private ArraySegment<T> _segment;
    private T[]? _dataArray;
    private bool _disposable;

    private RentedArray(T[] data, ArrayPool<T>? pool, int size)
    {
        SetInternalVariables(data, pool, size);
    }

    private void SetInternalVariables(T[] data, ArrayPool<T>? pool, int size)
    {
        _pool = pool;
        _disposable = size > 0 && pool != null;
        _dataArray = data;
        if (size == 0)
        {
            _segment = ArraySegment<T>.Empty;
            _data = ReadOnlyMemory<T>.Empty;
        }
        else
        {
            _segment = new ArraySegment<T>(_dataArray = data, 0, size);
            _data = new ReadOnlyMemory<T>(_dataArray, 0, size);
        }
    }

    public bool IsPooled
    {
        get
        {
            ThrowIfDisposed();

            return _pool is not null;
        }
    }

    public ReadOnlyMemory<T> ReadOnlyMemory
    {
        get
        {
            var data = _data;
            ThrowIfDisposed();

            return data;
        }
    }

    public int Length => ArraySegment.Count;

    private void ThrowIfDisposed()
    {
        if (_dataArray is null) throw new ObjectDisposedException(nameof(RentedArray<T>));
    }

    public ArraySegment<T> ArraySegment
    {
        get
        {
            var data = _segment;
            ThrowIfDisposed();

            return data;
        }
    }

    public static RentedArray<T> Empty { get; } = new RentedArray<T>(Array.Empty<T>(), null, 0);


    public void Dispose()
    {
        if (!_disposable) return;
        var data = _dataArray;
        if (data is not null && IsPooled)
        {
            _pool!.Return(data);
        }

        _dataArray = null;
    }

    public static RentedArray<T> FromPool(int minimumSize, ArrayPool<T>? pool, bool forBuffer = false)
    {
        var allocatedArray = pool?.Rent(minimumSize) ?? new T[minimumSize];
        return new RentedArray<T>(allocatedArray, pool, forBuffer ? allocatedArray.Length : minimumSize);
    }

    public static RentedArray<T> FromDefaultPool(int minimumSize, bool forBuffer = false)
    {
        return FromPool(minimumSize, ArrayPool<T>.Shared, forBuffer);
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

    public RentedArray<T> Clone(ArrayPool<T>? pool = null, bool useDefaultPool = false, bool forBuffer = false)
    {
        var newArray = FromPool(this.Length, pool ?? (useDefaultPool ? ArrayPool<T>.Shared :  _pool), forBuffer);
        ArraySegment.CopyTo(newArray.ArraySegment);
        return newArray;
    }

    private static void Resize(RentedArray<T> rentedArray, int newSize, bool keepData, bool forBuffer)
    {
        if (rentedArray.Length == newSize) return;
        var next = Array.Empty<T>();
        if (newSize > 0)
        {
            next = rentedArray._pool?.Rent(newSize) ?? new T[newSize];
        }

        if (newSize > 0 && keepData)
        {
            var countToCopy = rentedArray.Length > newSize ? newSize : rentedArray.Length;
            rentedArray.ArraySegment[..countToCopy].CopyTo(next);
        }

        if (!ReferenceEquals(rentedArray._dataArray, next))
            rentedArray._pool?.Return(rentedArray._dataArray!);
        rentedArray.SetInternalVariables(next, rentedArray._pool, forBuffer ? next.Length : newSize);
    }

    public void Resize(int size, bool keepData = true, bool forBuffer = false)
    {
        ThrowIfDisposed();
        if (size < 0) throw new ArgumentOutOfRangeException(nameof(size), "Size must be greater than or equal to zero");
        // Resizing to the same size is a no-op.
        if (size == _dataArray!.Length) return;

        // Allocate a new buffer of size, when our internal buffer isn't enough.
        if (size > _dataArray!.Length)
        {
            Resize(this, size, keepData, forBuffer);
            return;
        }

        if (size < _dataArray!.Length / 2)
        {
            Resize(this, size, keepData, forBuffer);
        }


        // When size is 0, or less than our buffer, rather than re-allocate
        // We can just set our size, and setup our segment and read only memory.
        SetInternalVariables(_dataArray!, _pool, forBuffer ? Length : size);
    }
}
