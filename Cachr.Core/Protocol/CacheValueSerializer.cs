using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Cachr.Core.Protocol;

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