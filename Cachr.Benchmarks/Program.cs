// See https://aka.ms/new-console-template for more information

using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Cachr.Core.Protocol;
using Newtonsoft.Json;

if (!args.Contains("--filter"))
{
    args = args.Append("--filter").Append("*").ToArray();
}


BenchmarkSwitcher.FromAssembly(typeof(ProtocolSerializerBenchmark).Assembly)
#if DEBUG
    .Run(args, new DebugInProcessConfig());
#else
    .Run(args);
#endif

[MemoryDiagnoser]
public class ProtocolSerializerBenchmark
{
    static ProtocolSerializerBenchmark()
    {
        _cacheValueSerializer = new CacheValueSerializer();
        _cacheCommandSerializer = new CacheCommandSerializer(_cacheValueSerializer);
        _cacheValue = new MapCacheValue(new[]
        {
            new KeyValuePairCacheValue(new StringCacheValue("Key1"),
                new SetCacheValue(new CacheValue[]
                {
                    new IntegerCacheValue(42), new UnsignedIntegerCacheValue(43), new StringCacheValue("44"),
                    new FloatCacheValue(42.1), NullCacheValue.Instance
                }.ToImmutableArray())),
        });
        _cacheCommand = new CacheCommand
        {
            CommandType = CacheCommandType.Set,
            Arguments = new[] {_cacheValue, new FloatCacheValue(Random.Shared.NextDouble())}.ToImmutableArray(),
            CommandReferenceId = -1
        };
    }

    private static readonly IProtocolSerializer<CacheValue> _cacheValueSerializer;
    private static readonly IProtocolSerializer<CacheCommand> _cacheCommandSerializer;

    [Benchmark(Baseline = true)]
    public async Task Baseline()
    {
        await Task.Delay(0);
    }

    private static readonly CacheValue _cacheValue;

    private static CacheCommand _cacheCommand;

    [Benchmark]
    public async Task CacheValueSerializeCustom()
    {
        var bytes = await _cacheValueSerializer.GetBytesAsync(_cacheValue);
        await _cacheValueSerializer.ReadBytesAsync(bytes);
    }

    [Benchmark]
    public async Task CacheCommandSerializeCustom()
    {
        var bytes = await _cacheCommandSerializer.GetBytesAsync(_cacheCommand);
        await _cacheCommandSerializer.ReadBytesAsync(bytes);
    }

    private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings()
    {
        TypeNameHandling = TypeNameHandling.Auto,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
        ConstructorHandling = ConstructorHandling.Default,
        Formatting = Formatting.None
    };
}
