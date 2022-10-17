using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using BenchmarkDotNet.Attributes;
using Cachr.Core.Buffers;
using Cachr.UnitTests;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;

namespace Cachr.Benchmarks;

[JsonExporterAttribute.Full]
[JsonExporterAttribute.FullCompressed]
[MemoryDiagnoser]
[ThreadingDiagnoser]
[SuppressMessage("ReSharper", "ClassCanBeSealed.Global", Justification = "Required by BenchmarkDotNet")]
public class WebSocketBenchmarks
{
    private readonly WebApplicationFactory<global::Program> _factory;

    public WebSocketBenchmarks()
    {
        var testApplication = TestHostBuilder.GetTestApplication()
            .WithWebHostBuilder(static b =>
                b.ConfigureLogging(static lb => lb.ClearProviders())
            );
        _factory = testApplication;
        _ = testApplication.Server.ToString(); // Force server to start, right now.
    }

    [Params(4096)]
    public int BufferSize { get; set; }

    private const int TotalSize = 16384;

    [Benchmark]
    public async Task WebSocketSendBenchmark()
    {
        await RunBenchmarkAsync(_factory.Server, BufferSize, TotalSize);
    }

    private static async Task RunBenchmarkAsync(TestServer testServer,
        int bufferSize,
        int totalSize,
        bool secure = false)
    {
        var webSocketClient = testServer.CreateWebSocketClient();
        using var socket = await webSocketClient.ConnectAsync(
            new Uri(
                $"{(secure ? "wss" : "ws")}://testhost/$cachr/$bus?id={Guid.NewGuid()}&uri={Uri.EscapeDataString("http://localhost:5000")}"),
            CancellationToken.None).ConfigureAwait(false);
        var yugeBuffer = RentedArray<byte>.FromDefaultPool(bufferSize);
        int bytesSent = 0;
        Random.Shared.NextBytes(yugeBuffer.ArraySegment.Array!);
        while (bytesSent < totalSize)
        {
            using var buffer = RentedArray<byte>.FromDefaultPool(Math.Min(bufferSize, totalSize - bytesSent));
            bytesSent += buffer.Length;
            await socket.SendAsync(yugeBuffer.ArraySegment, WebSocketMessageType.Binary, false, CancellationToken.None);
        }

        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Benchmark Complete", CancellationToken.None);
    }

    [Benchmark(OperationsPerInvoke = 512)]
    public async Task WebSocket512ConcurrentClientBenchmark()
    {
        await Task.WhenAll(Enumerable.Range(0, 512).Select(_ => RunBenchmarkAsync(_factory.Server, BufferSize, TotalSize)).ToArray());
    }

    [GlobalCleanup]
    public void DisposeTestHost()
    {
        _factory.Dispose();
    }
}
