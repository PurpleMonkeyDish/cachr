using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Cachr.Core.Messaging;

namespace Cachr.Benchmarks;

[JsonExporterAttribute.Full]
[JsonExporterAttribute.FullCompressed]
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class MessageBusBenchmarks
{
    private sealed record AwaitableCompletableMessage(long StartTimestamp) : ICompletableMessage
    {
        public AwaitableCompletableMessage() : this(DateTimeOffset.Now.ToUnixTimeMilliseconds()) { }

        private readonly TaskCompletionSource<long> _taskCompletionSource = new();
        public TaskAwaiter<long> GetAwaiter() => _taskCompletionSource.Task.GetAwaiter();

        public ValueTask CompleteAsync()
        {
            if (_taskCompletionSource.Task.IsCompleted) return ValueTask.CompletedTask;
            var timeTakenMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds() - StartTimestamp;
            _taskCompletionSource.TrySetResult(timeTakenMilliseconds);
            return ValueTask.CompletedTask;
        }

        public AwaitableCompletableMessage Reset() => new AwaitableCompletableMessage();
    }

    private MessageBus<object> _messageBus;
    private AwaitableCompletableMessage[] _messages = Array.Empty<AwaitableCompletableMessage>();
    private ISubscriptionToken[] _subscriptionTokens = Array.Empty<ISubscriptionToken>();
    [Params(1, 20, 100)] public int MessageCount { get; set; }
    [Params(100, 1000, 10000)] public int SubscriberCount { get; set; }

    [Benchmark]
    public async Task BroadcastAsyncBenchmark()
    {
        await Task.WhenAll(
            Enumerable.Range(0, MessageCount)
                .Select(i => _messageBus.BroadcastAsync(_messages[i], CancellationToken.None)
                    .ContinueWith(
                        async t =>
                        {
                            await t;
                            await _messages[i];
                            _messages[i] = _messages[i].Reset();
                        }
                    ).Unwrap()
                )
        );
    }


    [Benchmark]
    public async Task SendToAsyncBenchmark()
    {
        await Task.WhenAll(Enumerable.Range(0, MessageCount).Select(i => _messageBus
            .SendToRandomAsync(_messages[i], CancellationToken.None).ContinueWith(
                async t =>
                {
                    await t;
                    await _messages[i];
                    _messages[i] = _messages[i].Reset();
                })));

        Parallel.For(0, MessageCount, x =>
        {
            _messages[x] = _messages[x].Reset();
        });
    }

    [GlobalSetup]
    public void Setup()
    {
        _messageBus = new MessageBus<object>(new MessageBusOptions());
        _messages = Enumerable.Range(0, MessageCount).Select(i => new AwaitableCompletableMessage()).ToArray();
        _subscriptionTokens = Enumerable.Range(0, SubscriberCount)
            .Select(x => _messageBus.Subscribe(o => ValueTask.CompletedTask))
            .ToArray();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        for (var x = 0; x < SubscriberCount; x++)
        {
            _subscriptionTokens[x].Dispose();
        }

        _subscriptionTokens = Array.Empty<ISubscriptionToken>();
        ((MessageBus<object>)_messageBus).ShutdownAsync().GetAwaiter().GetResult();
    }
}
