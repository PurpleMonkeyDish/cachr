namespace Cachr.Core.Protocol;

public class CacheCommand
{
    public required ulong CommandId { get; init; }
    public required Command Command { get; init; }
    public required ProtocolValue[] Arguments { get; init; }
}
