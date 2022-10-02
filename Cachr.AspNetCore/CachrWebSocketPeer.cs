using System.Buffers;
using System.Net.WebSockets;
using Cachr.Core;
using Cachr.Core.Buffers;
using Cachr.Core.Peering;
using Microsoft.Extensions.Logging;

namespace Cachr.AspNetCore;

public sealed class CachrWebSocketPeer : IPeerConnection, IDisposable
{
    private const int OneKilobyte = 1024;
    private const int OneMegabyte = OneKilobyte * 1024;
    private readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1, 1);
    private readonly WebSocket _webSocket;
    private readonly ILogger _logger;
    private static readonly ArrayPool<byte> s_clientSocketPool = ArrayPool<byte>.Create(OneMegabyte, 32);
    public Guid Id { get; }
    public bool Enabled { get; set; }
    public Uri Uri { get; }

    public CachrWebSocketPeer(
        Guid id,
        Uri uri,
        WebSocket webSocket,
        ILoggerFactory loggerFactory
    )
    {
        Id = id;
        Uri = uri;
        _webSocket = webSocket;
        _logger = loggerFactory.CreateLogger(string.Join(".", typeof(CachrWebSocketPeer).FullName, Id.ToString("n")));
    }

    public async Task RunPeerAsync(CancellationToken cancellationToken)
    {
        const int InitialBufferSize = 4096;
        var receivedBytes = 0;
        var buffer = RentedArray<byte>.FromPool(InitialBufferSize, s_clientSocketPool);
        try
        {
            while (_webSocket.CloseStatus == null)
            {
                var receiveResult =
                    await _webSocket.ReceiveAsync(buffer.ArraySegment[receivedBytes..], cancellationToken);

                if (receiveResult.CloseStatus != null)
                {
                    break;
                }

                receivedBytes += receiveResult.Count;

                if (!receiveResult.EndOfMessage)
                {
                    if (receivedBytes >= buffer.Length - 1536)
                    {
                        buffer.Resize(buffer.Length * 2);
                    }

                    continue;
                }

                if (!Enabled)
                {
                    receivedBytes = 0;
                    continue;
                }

                var resultBuffer = RentedArray<byte>.FromDefaultPool(receivedBytes);
                buffer.ArraySegment[..receivedBytes].CopyTo(resultBuffer.ArraySegment);
                buffer.Dispose();
                buffer = RentedArray<byte>.FromPool(InitialBufferSize, s_clientSocketPool);
            }

            var source = new CancellationTokenSource();
            _logger.LogInformation("Web socket communication ended by peer.");
            source.CancelAfter(TimeSpan.FromSeconds(5));
            if (_webSocket.CloseStatus != null)
            {
                await _webSocket.CloseAsync(_webSocket.CloseStatus.Value,
                    _webSocket.CloseStatusDescription,
                    source.Token);
            }
            else
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,
                    nameof(WebSocketCloseStatus.NormalClosure),
                    source.Token);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "An exception occurred while reading data from our peer. The web socket will now be closed.");
        }
        finally
        {
            if (_webSocket.State != WebSocketState.Closed)
            {
                _logger.LogWarning(
                    "WebSocket loop ended, but the connection was left open. The connection will be aborted.");
            }

            buffer.Dispose();
        }
    }


    public async ValueTask SendAsync(RentedArray<byte> peerMessageData, CancellationToken cancellationToken)
    {
        try
        {
            using var _ = await _sendSemaphore.AcquireAsync(cancellationToken);
            await _webSocket.SendAsync(peerMessageData.ReadOnlyMemory,
                WebSocketMessageType.Binary,
                true,
                cancellationToken);
        }
        catch (ObjectDisposedException) { }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "An unexpected exception occurred when writing data.");
            if (_webSocket.CloseStatus != null) return;
            await _webSocket.CloseAsync(
                WebSocketCloseStatus.InternalServerError,
                "Internal server error during send",
                cancellationToken
            ).IgnoreExceptions();
        }
    }

    public ValueTask CloseAsync()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        _sendSemaphore.Dispose();
        _webSocket.Dispose();
    }
}
