using Cachr.Core.Buffers;

namespace Cachr.Core.Serializers;

public interface ISerializer<T> where T : class
{
    RentedArray<byte> Serialize(T? obj);
    T? Deserialize(RentedArray<byte> data);
}