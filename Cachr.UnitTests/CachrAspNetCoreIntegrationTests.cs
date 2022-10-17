using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Cachr.AspNetCore;
using Cachr.Core.Buffers;
using Xunit;
using Xunit.Abstractions;

namespace Cachr.UnitTests;

public sealed class CachrAspNetCoreIntegrationTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public CachrAspNetCoreIntegrationTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [InlineData(WebSocketCloseStatus.NormalClosure)]
    [InlineData(WebSocketCloseStatus.EndpointUnavailable)]
    [InlineData(WebSocketCloseStatus.ProtocolError)]
    [InlineData(WebSocketCloseStatus.InvalidMessageType)]
    [InlineData(WebSocketCloseStatus.Empty)]
    [InlineData(WebSocketCloseStatus.InvalidPayloadData)]
    [InlineData(WebSocketCloseStatus.PolicyViolation)]
    [InlineData(WebSocketCloseStatus.MessageTooBig)]
    [InlineData(WebSocketCloseStatus.MandatoryExtension)]
    [InlineData(WebSocketCloseStatus.InternalServerError)]
    public async Task GracefulConnectionCloseReturnsExpectedCloseStatus(WebSocketCloseStatus expectedCloseStatus)
    {
        var expectedStatusDescription = $"Unit Test - {expectedCloseStatus} - {Guid.NewGuid()}";
        var testApplication = TestHostBuilder.GetTestApplication();
        var webSocketClient = testApplication.Server.CreateWebSocketClient();
        var socket = await webSocketClient.ConnectAsync(
            new Uri(
                $"ws://testhost/$cachr/$bus?id={Guid.NewGuid()}&uri={Uri.EscapeDataString("http://localhost:5000")}"),
            CancellationToken.None);
        await socket.SendAsync(ArraySegment<byte>.Empty, WebSocketMessageType.Binary, true, CancellationToken.None);
        await socket.CloseAsync(expectedCloseStatus, expectedStatusDescription, CancellationToken.None);
        Assert.NotNull(socket.CloseStatus);
        Assert.NotNull(socket.CloseStatusDescription);
        Assert.Equal(expectedCloseStatus, socket.CloseStatus.Value);
        Assert.Equal(expectedStatusDescription, socket.CloseStatusDescription);
    }

    [Fact]
    public async Task LargePayloadsWorkProperly()
    {
        var testApplication = TestHostBuilder.GetTestApplication();
        var webSocketClient = testApplication.Server.CreateWebSocketClient();
        var socket = await webSocketClient.ConnectAsync(
            new Uri(
                $"ws://testhost/$cachr/$bus?id={Guid.NewGuid()}&uri={Uri.EscapeDataString("http://localhost:5000")}"),
            CancellationToken.None);
        var yugeBuffer = RentedArray<byte>.FromDefaultPool(1024 * 1024);
        // Send 512MB of data, 1MB at a time.
        for (var x = 0; x < 511; x++)
        {
            Random.Shared.NextBytes(yugeBuffer.ArraySegment.Array!);
            await socket.SendAsync(yugeBuffer.ArraySegment, WebSocketMessageType.Binary, false, CancellationToken.None);
        }
        await socket.SendAsync(yugeBuffer.ArraySegment, WebSocketMessageType.Binary, true, CancellationToken.None);
        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test Complete", CancellationToken.None);
    }
}
