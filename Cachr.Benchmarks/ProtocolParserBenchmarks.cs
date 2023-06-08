using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Cachr.Core;
using Cachr.Core.Protocol;

namespace Cachr.Benchmarks;

[MemoryDiagnoser]
public class ProtocolParserBenchmarks
{
    private static ICacheProtocolCommandParser _commandParser = new CacheProtocolCommandParser();
    private static byte[] _encodedCommand = GeneratePayload();
    [Benchmark(Baseline = true)]
    public void BaselineBenchmark()
    {
        var obj = new object();
        GC.KeepAlive(obj);
    }

    private static byte[] GeneratePayload()
    {
        var someRandomBytes = new byte[] {42, 36, 128, 255, 12, 79, 65, 233};
        var commandPacket = new CacheCommand()
        {
            Arguments = new[]
            {
                new ProtocolValue(CacheType.CommandPayload, someRandomBytes),
                new ProtocolValue(CacheType.CommandPayload, someRandomBytes),
                new ProtocolValue(CacheType.CommandPayload, someRandomBytes),
                new ProtocolValue(CacheType.CommandPayload, someRandomBytes),
                new ProtocolValue(CacheType.CommandPayload, someRandomBytes),
                new ProtocolValue(CacheType.CommandPayload, someRandomBytes),
                new ProtocolValue(CacheType.CommandPayload, someRandomBytes),
                new ProtocolValue(CacheType.CommandPayload, someRandomBytes),
                new ProtocolValue(CacheType.CommandPayload, someRandomBytes),
                new ProtocolValue(CacheType.CommandPayload, someRandomBytes),
                new ProtocolValue(CacheType.CommandPayload, someRandomBytes),
            },
            Command = Command.Batch,
            CommandId = ulong.MaxValue
        };

        return _commandParser.ToByteArrayAsync(commandPacket).GetAwaiter().GetResult();
    }
    [Benchmark]
    public async Task Encode()
    {
        var someRandomBytes = new byte[] {42, 36, 128, 255, 12, 79, 65, 233};
        var commandPacket = new CacheCommand()
        {
            Arguments = new[]
            {
                new ProtocolValue(CacheType.CommandPayload, someRandomBytes),
                new ProtocolValue(CacheType.CommandPayload, someRandomBytes),
                new ProtocolValue(CacheType.CommandPayload, someRandomBytes),
                new ProtocolValue(CacheType.CommandPayload, someRandomBytes),
                new ProtocolValue(CacheType.CommandPayload, someRandomBytes),
                new ProtocolValue(CacheType.CommandPayload, someRandomBytes),
                new ProtocolValue(CacheType.CommandPayload, someRandomBytes),
                new ProtocolValue(CacheType.CommandPayload, someRandomBytes),
                new ProtocolValue(CacheType.CommandPayload, someRandomBytes),
                new ProtocolValue(CacheType.CommandPayload, someRandomBytes),
                new ProtocolValue(CacheType.CommandPayload, someRandomBytes),
            },
            Command = Command.Batch,
            CommandId = ulong.MaxValue
        };

        var encoded = await _commandParser.ToByteArrayAsync(commandPacket);
        GC.KeepAlive(encoded);
    }

    [Benchmark]
    public async Task Decode()
    {
        _commandParser.TryParse(_encodedCommand, out _, out _);
    }

}
