using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using Cachr.Core;
using Cachr.Core.Messages.Duplication;
using Cachr.Core.Messaging;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cachr.Benchmarks;

[JsonExporterAttribute.Full]
[JsonExporterAttribute.FullCompressed]
[MemoryDiagnoser]
[SuppressMessage("ReSharper", "ClassCanBeSealed.Global", Justification = "Required by BenchmarkDotNet")]
public class EndToEndBenchmarks
{
    public EndToEndBenchmarks()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        serviceCollection.AddCachr(options => { });
        serviceCollection.AddSingleton<CachrMessageReflector>();
        _services = serviceCollection.BuildServiceProvider();
        _reflector = _services.GetRequiredService<CachrMessageReflector>(); // Make sure the message reflector gets started.
        _cache = _services.GetRequiredService<IDistributedCache>();
    }

    private readonly IServiceProvider _services;
    private CachrMessageReflector _reflector;
    private readonly IDistributedCache _cache;

    public class CachrMessageReflector : IDisposable
    {
        private readonly IMessageBus<InboundCacheMessageEnvelope> _inboundMessageBus;
        private readonly IDuplicateTracker<Guid> _duplicateTracker;
        private readonly ISubscriptionToken _subscriptionToken;
        public CachrMessageReflector(
            IMessageBus<OutboundCacheMessageEnvelope> outboundMessageBus,
            IMessageBus<InboundCacheMessageEnvelope> inboundMessageBus,
            IDuplicateTracker<Guid> duplicateTracker
            )
        {
            _inboundMessageBus = inboundMessageBus;
            _duplicateTracker = duplicateTracker;
            _subscriptionToken = outboundMessageBus.Subscribe(OnOutboundMessage);
        }

        private async Task OnOutboundMessage(OutboundCacheMessageEnvelope arg)
        {
            if (_duplicateTracker.IsDuplicate(arg.Message.Id)) return;
            await _inboundMessageBus.BroadcastAsync(new InboundCacheMessageEnvelope(NodeIdentity.Id, arg.Target,
                arg.Message));
        }

        public void Dispose()
        {
            _subscriptionToken.Dispose();
        }
    }

    [Benchmark]
    public async Task EndToEndCachePerformanceAsync()
    {
        for (var x = 0; x < 100; x++)
        {
            await _cache.SetAsync(string.Empty, Array.Empty<byte>());
        }
    }

    [Benchmark]
    public void CacheSetBenchmark()
    {
        _cache.Set(string.Empty, Array.Empty<byte>());
    }

    [Benchmark]
    public async Task CacheSetBenchmarkAsync()
    {
        await _cache.SetAsync(string.Empty, Array.Empty<byte>());
    }

    [Benchmark]
    public byte[] CacheGetBenchmark()
    {
        return _cache.Get(string.Empty);
    }

    [Benchmark]
    public async Task<byte[]> CacheGetBenchmarkAsync()
    {
        return await _cache.GetAsync(string.Empty);
    }

    [Benchmark]
    public void CacheRefreshBenchmark()
    {
        _cache.Set(string.Empty, Array.Empty<byte>());
        _cache.Refresh(string.Empty);
    }

    [Benchmark]
    public async Task CacheRefreshBenchmarkAsync()
    {
        await _cache.SetAsync(string.Empty, Array.Empty<byte>());
        await _cache.RefreshAsync(string.Empty);
    }

    [Benchmark]
    public void CacheRemoveBenchmark()
    {
        _cache.Set(string.Empty, Array.Empty<byte>());
        _cache.Remove(string.Empty);
    }


    [Benchmark]
    public async Task CacheRemoveAsyncBenchmark()
    {
        await _cache.SetAsync(string.Empty, Array.Empty<byte>());
        await _cache.RemoveAsync(string.Empty);
    }



    public void Dispose()
    {
        (_services as IDisposable)?.Dispose();
    }
}
