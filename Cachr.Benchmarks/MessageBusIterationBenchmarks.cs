using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using Cachr.Core.Messaging;

namespace Cachr.Benchmarks;

[JsonExporterAttribute.Full]
[JsonExporterAttribute.FullCompressed]
[MemoryDiagnoser]
[ThreadingDiagnoser]
[SuppressMessage("ReSharper", "ClassCanBeSealed.Global", Justification = "Required by BenchmarkDotNet")]
public class MessageBusIterationBenchmarks
{
    private sealed record AwaitableCompletableMessage(long StartTimestamp) : ICompletableMessage
    {
        public AwaitableCompletableMessage() : this(DateTimeOffset.Now.ToUnixTimeMilliseconds()) { }

        private readonly TaskCompletionSource<long> _taskCompletionSource = new();

        public ConfiguredTaskAwaitable<long> ConfigureAwait(bool continueOnCapturedContext) =>
            _taskCompletionSource.Task.ConfigureAwait(continueOnCapturedContext);

        public TaskAwaiter<long> GetAwaiter() => _taskCompletionSource.Task.GetAwaiter();

        public void Complete()
        {
            if (_taskCompletionSource.Task.IsCompleted) return;
            var timeTakenMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds() - StartTimestamp;
            _taskCompletionSource.TrySetResult(timeTakenMilliseconds);
        }

        public AwaitableCompletableMessage Reset() => new AwaitableCompletableMessage();
    }

    private MessageBus<object>? _messageBus;
    private AwaitableCompletableMessage[] _messages = Array.Empty<AwaitableCompletableMessage>();
    private ISubscriptionToken[] _subscriptionTokens = Array.Empty<ISubscriptionToken>();
    private const int MessageCount = 100;
    private const int SubscriberCount = 10000;

    [Benchmark(OperationsPerInvoke = MessageCount)]
    public async Task BroadcastAsyncBenchmark()
    {
        await Task.WhenAll(
            Enumerable.Range(0, MessageCount)
                .Select(i => _messageBus!.BroadcastAsync(_messages[i], CancellationToken.None)
                    .ContinueWith(
                        async t =>
                        {
                            await t.ConfigureAwait(false);
                            await _messages[i];
                            _messages[i] = _messages[i].Reset();
                        }
                    ).Unwrap()
                )
        ).ConfigureAwait(false);
    }


    [Benchmark(OperationsPerInvoke = MessageCount)]
    public async Task SendToAsyncBenchmark()
    {
        await Task.WhenAll(Enumerable.Range(0, MessageCount).Select(i => _messageBus!
            .SendToRandomAsync(_messages[i], CancellationToken.None).ContinueWith(
                async t =>
                {
                    await t.ConfigureAwait(false);
                    await _messages[i].ConfigureAwait(false);
                    _messages[i] = _messages[i].Reset();
                }))).ConfigureAwait(false);

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
        _subscriptionTokens = Enumerable.Range(0, SubscriberCount / 4)
            .SelectMany(x => new[]
            {
                _messageBus.Subscribe(o => ValueTask.CompletedTask, (SubscriptionMode)0),
                _messageBus.Subscribe(o => ValueTask.CompletedTask, SubscriptionMode.Broadcast),
                _messageBus.Subscribe(o => ValueTask.CompletedTask, SubscriptionMode.Targeted),
                _messageBus.Subscribe(o => ValueTask.CompletedTask, SubscriptionMode.All),
            })
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
        _messageBus!.ShutdownAsync().GetAwaiter().GetResult();
    }
}
