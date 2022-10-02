using System.Text.Json;
using Cachr.Core.Buffers;

namespace Cachr.Core.Serializers;

public interface ISerializer<T> where T : class
{
    RentedArray<byte> Serialize(T? obj);
    T? Deserialize(RentedArray<byte> data);
}
public class DefaultSerializer<T> : ISerializer<T> where T : class
{
    public RentedArray<byte> Serialize(T? obj)
    {
        var result = JsonSerializer.SerializeToUtf8Bytes(obj);
        var rentedArray = RentedArray<byte>.FromDefaultPool(result.Length);
        result.CopyTo(rentedArray.ArraySegment.AsSpan());
        return rentedArray;
    }

    public T? Deserialize(RentedArray<byte> data)
    {
        using var utf8Stream = data.ToMemoryStream(false);
        return JsonSerializer.Deserialize<T>(utf8Stream);
    }
}
