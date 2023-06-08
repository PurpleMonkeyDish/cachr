namespace Cachr.Core.Protocol;

public interface ICacheProtocolCommandParser : ICommandParser
{
    bool TryParse(ReadOnlySpan<byte> bytes, out CacheCommand? command, out int bytesConsumed);

    ValueTask WriteToStreamAsync(CacheCommand cacheCommand,
        Stream stream,
        CancellationToken cancellationToken);

    Task<byte[]> ToByteArrayAsync(CacheCommand cacheCommand, CancellationToken cancellationToken = default);
}