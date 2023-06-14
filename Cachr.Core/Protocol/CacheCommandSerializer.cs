using System.Buffers;
using System.Collections.Immutable;

namespace Cachr.Core.Protocol;

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