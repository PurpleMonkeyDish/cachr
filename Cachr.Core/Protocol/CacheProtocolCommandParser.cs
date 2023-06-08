using System.Buffers;
using System.Collections.Immutable;

namespace Cachr.Core.Protocol;

// Native packet format:
// 7Bit encoded version
// 7Bit encoded command reference ID
// 7Bit encoded command
// 7Bit encoded parameter count
// Repeat:
// 7Bit encoded type
// 7Bit encoded arg length
// Length bytes arg data

// Minimum packet length: 4
public sealed class CacheProtocolCommandParser : ICacheProtocolCommandParser
{
    private const int MinimumPacketLength = 4;
    private const ulong CurrentVersion = 1;

    // ReSharper disable SuspiciousTypeConversion.Global
    private static readonly ImmutableHashSet<ulong> _validCommandIds =
        Enum.GetValues<Command>().Select(i => Convert.ToUInt64(i)).ToImmutableHashSet();

    private static readonly ImmutableHashSet<ulong> _validTypeIds =
        Enum.GetValues<CacheType>().Select(i => Convert.ToUInt64(i)).ToImmutableHashSet();
    // ReSharper restore SuspiciousTypeConversion.Global

    public Task<CacheCommand> FromStreamAsync(Stream stream)
    {
        throw new NotImplementedException();
    }
    public bool TryParse(ReadOnlySpan<byte> bytes, out CacheCommand? command, out int bytesConsumed)
    {
        command = default;
        bytesConsumed = default;
        if (bytes.Length < MinimumPacketLength) return false;

        if (!BitEncoder.TryDecode7BitUInt64(bytes, out var version, out var bytesRead)) return false;
        bytesConsumed += bytesRead;
        bytes = bytes[bytesRead..];
        if (version == 0 || version > CurrentVersion)
            throw new InvalidOperationException($"Unsupported protocol version {version}");
        if (!BitEncoder.TryDecode7BitUInt64(bytes, out var commandId, out bytesRead))
            return false;
        bytesConsumed += bytesRead;
        bytes = bytes[bytesRead..];
        if (!BitEncoder.TryDecode7BitUInt64(bytes, out var commandValue, out bytesRead)) return false;
        bytesConsumed += bytesRead;
        bytes = bytes[bytesRead..];
        if (!_validCommandIds.Contains(commandValue))
        {
            throw new InvalidOperationException($"Invalid command ID: {commandValue}");
        }

        if (!BitEncoder.TryDecode7BitUInt64(bytes, out var parameterCount, out bytesRead)) return false;
        bytesConsumed += bytesRead;
        bytes = bytes[bytesRead..];
        var parameterArray = new ProtocolValue[parameterCount];

        for (var i = 0; i < (long)parameterCount; i++)
        {
            if (!BitEncoder.TryDecode7BitUInt64(bytes, out var parameterType, out bytesRead))
                return false;

            if (!_validTypeIds.Contains(parameterType))
                throw new InvalidOperationException($"Invalid data type id: {parameterType}");
            bytes = bytes[bytesRead..];
            bytesConsumed += bytesRead;
            if (!BitEncoder.TryDecode7BitInt64(bytes, out var length, out bytesRead))
                return false;

            bytes = bytes[bytesRead..];
            bytesConsumed += bytesRead;
            if (length == -1)
            {
                // -1 indicates null.
                parameterArray[i] = new ProtocolValue((CacheType)parameterType, null);
                continue;
            }

            if (length < 0)
            {
                throw new InvalidOperationException("Protocol violation: Length must be >= -1");
            }

            if (bytes.Length < length)
                return false;
            bytesRead = (int)length;
            bytesConsumed += bytesRead;

            parameterArray[i] = new ProtocolValue((CacheType)parameterType, bytes[..bytesRead].ToArray());
            bytes = bytes[bytesRead..];
        }

        // We specifically do not validate command ID, as it is for the caller to track asynchronous calls.
        // it means nothing to us, other than to echo it back when we respond to this command.
        command = new CacheCommand {CommandId = commandId, Command = (Command)commandValue, Arguments = parameterArray};

        return true;
    }

    public async ValueTask WriteToStreamAsync(CacheCommand cacheCommand,
        Stream stream,
        CancellationToken cancellationToken)
    {
        // Cheating, write the version as 1. This is what 7 bit encoded unsigned 1 is anyway.
        var buffer = ArrayPool<byte>.Shared.Rent(1024);
        Array.Clear(buffer);
        try
        {
            var totalBytes = 0;
            totalBytes += BitEncoder.Encode7Bit(buffer.AsSpan()[totalBytes..], CurrentVersion);
            totalBytes += BitEncoder.Encode7Bit(buffer.AsSpan()[totalBytes..], cacheCommand.CommandId);
            totalBytes += BitEncoder.Encode7Bit(buffer.AsSpan()[totalBytes..], (ulong)cacheCommand.Command);
            totalBytes += BitEncoder.Encode7Bit(buffer.AsSpan()[totalBytes..], (ulong)cacheCommand.Arguments.Length);
            foreach (var argument in cacheCommand.Arguments)
            {
                totalBytes += BitEncoder.Encode7Bit(buffer.AsSpan()[totalBytes..], (ulong)argument.Type);
                totalBytes +=
                    BitEncoder.Encode7Bit(buffer.AsSpan()[totalBytes..], (long)(argument.Value?.Length ?? -1));
                if (argument.Value is null)
                {
                    if (totalBytes > 800)
                    {
                        await stream.WriteAsync(buffer.AsMemory(0, totalBytes), cancellationToken);
                        totalBytes = 0;
                        Array.Clear(buffer);
                    }

                    continue;
                }

                await stream.WriteAsync(buffer.AsMemory(0, totalBytes), cancellationToken);
                await stream.WriteAsync(argument.Value.AsMemory(), cancellationToken);
                totalBytes = 0;
                Array.Clear(buffer);
            }

            if (totalBytes != 0)
            {
                await stream.WriteAsync(buffer.AsMemory(0, totalBytes), cancellationToken);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public async Task<byte[]> ToByteArrayAsync(CacheCommand cacheCommand, CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();
        await WriteToStreamAsync(cacheCommand, memoryStream, cancellationToken);
        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
        await memoryStream.FlushAsync(cancellationToken);
        var array = memoryStream.ToArray();
        return array;
    }
}
