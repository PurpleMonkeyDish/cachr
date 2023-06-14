using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Cachr.Core.Protocol;

public interface IProtocolSerializer<T>
{
    public int GetDesiredBufferSize(T obj);
    Task WriteAsync(BinaryWriter writer, T obj, CancellationToken cancellationToken);
    Task<T> ReadAsync(BinaryReader reader, CancellationToken cancellationToken);
}

public static class ProtocolSerializerExtensions {
    public static async Task<byte[]> GetBytesAsync<T>(this IProtocolSerializer<T> serializer,
        T value,
        CancellationToken cancellationToken = default)
    {
        var bufferSize = serializer.GetDesiredBufferSize(value);
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        using var memoryStream = new MemoryStream(buffer, true);
        memoryStream.Seek(0, SeekOrigin.Begin);
        await using var binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8, true);
        await serializer.WriteAsync(binaryWriter, value, cancellationToken).ConfigureAwait(false);
        binaryWriter.Flush();
        await memoryStream.FlushAsync(cancellationToken);
        return memoryStream.ToArray();
    }

    public static async Task<T> ReadBytesAsync<T>(this IProtocolSerializer<T> serializer, byte[] data, CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream(data, false);
        memoryStream.Seek(0, SeekOrigin.Begin);
        using var binaryReader = new BinaryReader(memoryStream, Encoding.UTF8, true);
        return await serializer.ReadAsync(binaryReader, cancellationToken);
    }
}
public enum CacheCommandType
{
    Invalid,
    Get,
    Set,
    SetExpiration,
    Remove,
    CompareExchange,
    Increment,
    Decrement,
    Ping
}

public enum CacheValueType
{
    Invalid,
    String,
    Integer,
    UnsignedInteger,
    Float,
    Bytes,
    Set,
    Map,
    KeyValuePair,
    Null
}

public class ProtocolViolationException : Exception
{
    public ProtocolViolationException(string message)
        : base(message)
    {

    }
}

public sealed class CacheValueSerializer : IProtocolSerializer<CacheValue>
{
    public int GetDesiredBufferSize(CacheValue obj) => obj.GetEstimatedSize();

    [SuppressMessage("ReSharper", "SwitchStatementHandlesSomeKnownEnumValuesWithDefault")]
    public Task WriteAsync(BinaryWriter writer, CacheValue root, CancellationToken cancellationToken)
    {
        // This queue is to avoid recursion.
        var localQueue = new Stack<CacheValue>();
        localQueue.Push(root);
        while (localQueue.TryPop(out var obj))
        {
            cancellationToken.ThrowIfCancellationRequested();
            writer.Write7BitEncodedInt((int)obj.ValueType);
            switch (obj.ValueType)
            {
                case CacheValueType.String:
                    writer.Write(((StringCacheValue)obj).Value);
                    break;
                case CacheValueType.Integer:
                    writer.Write7BitEncodedInt64(((IntegerCacheValue)obj).Value);
                    break;
                case CacheValueType.UnsignedInteger:
                    writer.Write7BitEncodedInt64((long)((UnsignedIntegerCacheValue)obj).Value);
                    break;
                case CacheValueType.Float:
                    writer.Write(((FloatCacheValue)obj).Value);
                    break;
                case CacheValueType.Bytes:
                    var bytesObject = (BytesCacheValue)obj;
                    writer.Write7BitEncodedInt(bytesObject.Value.Length);
                    writer.Write(bytesObject.Value.AsSpan());
                    break;
                case CacheValueType.Set:
                    var setObj = (SetCacheValue)obj;
                    writer.Write7BitEncodedInt(setObj.Value.Length);
                    foreach (var value in setObj.Value.Reverse())
                    {
                        localQueue.Push(value);
                    }
                    break;
                case CacheValueType.Map:
                    var mapObj = (MapCacheValue)obj;
                    writer.Write7BitEncodedInt(mapObj.Value.Count);
                    foreach (var keyValuePair in mapObj.Value.Reverse())
                    {
                        localQueue.Push(new KeyValuePairCacheValue(keyValuePair.Key, keyValuePair.Value));
                    }

                    break;
                case CacheValueType.KeyValuePair:
                    var kvpObj = (KeyValuePairCacheValue)obj;
                    localQueue.Push(kvpObj.Value);
                    localQueue.Push(kvpObj.Key);
                    break;
                case CacheValueType.Null:
                    break;
                default:
                    throw new System.Net.ProtocolViolationException($"Unknown value type: {obj.ValueType}");
            }
        }

        return Task.CompletedTask;
    }

    [SuppressMessage("ReSharper", "SwitchStatementHandlesSomeKnownEnumValuesWithDefault")]
    public async Task<CacheValue> ReadAsync(BinaryReader reader, CancellationToken cancellationToken)
    {
        var type = (CacheValueType)reader.Read7BitEncodedInt();
        switch (type)
        {
            case CacheValueType.String:
                return new StringCacheValue(reader.ReadString());
            case CacheValueType.Integer:
                return new IntegerCacheValue(reader.Read7BitEncodedInt64());
            case CacheValueType.UnsignedInteger:
                return new UnsignedIntegerCacheValue((ulong)reader.Read7BitEncodedInt64());
            case CacheValueType.Float:
                return new FloatCacheValue(reader.ReadDouble());
            case CacheValueType.Bytes:
                var byteCount = reader.Read7BitEncodedInt();
                var bytes = reader.ReadBytes(byteCount);
                return new BytesCacheValue(bytes.ToImmutableArray());
            case CacheValueType.Set:
                var setItemCount = reader.Read7BitEncodedInt();
                var items = new CacheValue[setItemCount];
                for (var i = 0; i < setItemCount; i++)
                {
                    items[i] = await ReadAsync(reader, cancellationToken);
                }

                return new SetCacheValue(items.ToImmutableArray());
            case CacheValueType.Map:
                var mapItemCount = reader.Read7BitEncodedInt();
                var keyValuePairs = new KeyValuePairCacheValue[mapItemCount];
                for (var i = 0; i < mapItemCount; i++)
                {
                    var next = await ReadAsync(reader, cancellationToken);
                    if (next is not KeyValuePairCacheValue kvpCacheValue)
                        throw new ProtocolViolationException($"Expected a key value pair, but got {next.ValueType}");
                    keyValuePairs[i] = kvpCacheValue;
                }

                return new MapCacheValue(keyValuePairs);
            case CacheValueType.KeyValuePair:
                var key = await ReadAsync(reader, cancellationToken);
                var value = await ReadAsync(reader, cancellationToken);
                return new KeyValuePairCacheValue(key, value);
            case CacheValueType.Null:
                return NullCacheValue.Instance;
            default:
                throw new ProtocolViolationException($"Invalid cache value type: {type}");
        }
    }
}

public sealed record NullCacheValue : CacheValue
{
    private NullCacheValue()
        : base(CacheValueType.Null)
    {

    }
    public static NullCacheValue Instance = new();
    protected override int GetValueMaxEncodedSize() => 0;
}

public sealed record KeyValuePairCacheValue : CacheValue
{
    public KeyValuePairCacheValue(CacheValue key, CacheValue value) : base(CacheValueType.KeyValuePair)
    {
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (key.ValueType)
        {
            case CacheValueType.String:
            case CacheValueType.Integer:
            case CacheValueType.UnsignedInteger:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(key), "Cache key must be an integer, unsigned integer, or string.");
        }

        Key = key;
        Value = value;
    }

    public CacheValue Value { get; init; }

    public CacheValue Key { get; init; }

    protected override int GetValueMaxEncodedSize() => Key.GetEstimatedSize() + Value.GetEstimatedSize();
}

public sealed record MapCacheValue : CacheValue
{
    public MapCacheValue(IEnumerable<KeyValuePairCacheValue> values)
        : base(CacheValueType.Map)
    {
        Value = values.ToImmutableDictionary(i => i.Key, i => i.Value);
    }

    public ImmutableDictionary<CacheValue, CacheValue> Value { get; init; }
    protected override int GetValueMaxEncodedSize() => ProtocolConstants.Max7BitEncodedIntBytes + Value.Sum(i => i.Key.GetEstimatedSize() + i.Value.GetEstimatedSize());
}

public sealed record SetCacheValue(ImmutableArray<CacheValue> Value) : CacheValue(CacheValueType.Set)
{
    protected override int GetValueMaxEncodedSize()
    {
        return ProtocolConstants.Max7BitEncodedIntBytes + Value.Select(i => i.GetEstimatedSize()).Sum();
    }
}

public sealed record BytesCacheValue(ImmutableArray<byte> Value) : CacheValue(CacheValueType.Bytes)
{
    protected override int GetValueMaxEncodedSize()
    {
        return ProtocolConstants.Max7BitEncodedIntBytes + Value.Length;
    }
}

public sealed record FloatCacheValue(double Value) : CacheValue(CacheValueType.Float)
{
    protected override int GetValueMaxEncodedSize() => 8;
}

public sealed record UnsignedIntegerCacheValue(ulong Value) : CacheValue(CacheValueType.UnsignedInteger)
{
    protected override int GetValueMaxEncodedSize() => ProtocolConstants.Max7BitEncodedIntBytes;
}

public sealed record IntegerCacheValue(long Value) : CacheValue(CacheValueType.Integer)
{
    protected override int GetValueMaxEncodedSize() => ProtocolConstants.Max7BitEncodedIntBytes;
}

public sealed record StringCacheValue(string Value) : CacheValue(CacheValueType.String)
{
    protected override int GetValueMaxEncodedSize() => Encoding.UTF8.GetByteCount(Value) + ProtocolConstants.Max7BitEncodedIntBytes;
}

public abstract record CacheValue(CacheValueType ValueType)
{
    internal int GetEstimatedSize() => ProtocolConstants.Max7BitEncodedIntBytes + GetValueMaxEncodedSize();
    protected abstract int GetValueMaxEncodedSize();
}

public class CacheCommandSerializer : IProtocolSerializer<CacheCommand>
{
    private readonly IProtocolSerializer<CacheValue> _valueSerializer;

    public CacheCommandSerializer(IProtocolSerializer<CacheValue> valueSerializer)
    {
        _valueSerializer = valueSerializer;
    }

    public int GetDesiredBufferSize(CacheCommand obj)
    {
        // We over estimate, always. Since this is used to rent from a pool.
        return (ProtocolConstants.Max7BitEncodedIntBytes * 4) + obj.Arguments.Sum(i => i.GetEstimatedSize());
    }

    public async Task WriteAsync(BinaryWriter writer, CacheCommand obj, CancellationToken cancellationToken)
    {
        writer.Write7BitEncodedInt64((long)ProtocolConstants.ProtocolVersion);
        writer.Write7BitEncodedInt((int)obj.CommandType);
        writer.Write7BitEncodedInt64(obj.CommandReferenceId);
        writer.Write7BitEncodedInt(obj.Arguments.Length);
        for (var i = 0; i < obj.Arguments.Length; i++)
        {
            await _valueSerializer.WriteAsync(writer, obj.Arguments[i], cancellationToken);
        }
    }

    public async Task<CacheCommand> ReadAsync(BinaryReader reader, CancellationToken cancellationToken)
    {
        var protocolVersion = (ulong)reader.Read7BitEncodedInt64();
        if (protocolVersion > ProtocolConstants.ProtocolVersion)
            throw new ProtocolViolationException($"Unsupported protocol version {protocolVersion}");
        var commandType = (CacheCommandType)reader.Read7BitEncodedInt();
        var commandId = reader.Read7BitEncodedInt64();
        var argumentCount = reader.Read7BitEncodedInt();
        var args = ArrayPool<CacheValue>.Shared.Rent(argumentCount);
        try
        {
            for (var i = 0; i < argumentCount; i++)
            {
                args[i] = await _valueSerializer.ReadAsync(reader, cancellationToken);
            }

            return new CacheCommand
            {
                CommandType = commandType,
                CommandReferenceId = commandId,
                Arguments = args.Take(argumentCount).ToImmutableArray()
            };
        }
        finally
        {
            ArrayPool<CacheValue>.Shared.Return(args);
        }
    }
}

public enum CacheCommandStatus
{
    Ok,
    Partial,
    PartialComplete,
    Error
}

public sealed class CacheCommandResponse
{
    public CacheCommandStatus Status { get; init; }
    public long CommandReferenceId { get; init; }
    public int Order { get; init; }
    public CacheValue Value { get; init; } = NullCacheValue.Instance;
}

internal static class ProtocolConstants
{
    public const ulong ProtocolVersion = 1;
    public const int Max7BitEncodedIntBytes = 10;
}

public sealed class CacheCommand
{
    public CacheCommandType CommandType { get; init; }
    public long CommandReferenceId { get; init; }
    public ImmutableArray<CacheValue> Arguments { get; init; }
}
