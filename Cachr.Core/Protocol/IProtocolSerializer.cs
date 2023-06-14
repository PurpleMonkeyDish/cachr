namespace Cachr.Core.Protocol;

public interface IProtocolSerializer<T>
{
    public int GetDesiredBufferSize(T obj);
    Task WriteAsync(BinaryWriter writer, T obj, CancellationToken cancellationToken);
    Task<T> ReadAsync(BinaryReader reader, CancellationToken cancellationToken);
}