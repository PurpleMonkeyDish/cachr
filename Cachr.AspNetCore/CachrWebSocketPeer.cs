using System.Buffers;
using System.Collections.Immutable;
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
    private WebSocket? _webSocket;
    private readonly ILogger _logger;
    private static readonly ArrayPool<byte> s_clientSocketPool = ArrayPool<byte>.Create(OneMegabyte, 32);
    public PeerDescription Description { get; }
    public bool Enabled { get; set; }

    public CachrWebSocketPeer(
        Guid id,
        Uri[] uris,
        WebSocket webSocket,
        ILoggerFactory loggerFactory
    )
    {
        Description = new PeerDescription(id, uris);
        _webSocket = webSocket;
        _logger = loggerFactory.CreateLogger(string.Join(".", typeof(CachrWebSocketPeer).FullName, Description.Id.ToString("n")));
    }

    public async Task RunPeerAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Web socket receive loop started");
        const int InitialBufferSize = OneKilobyte * 4;
        var receivedBytes = 0;
        using var buffer = RentedArray<byte>.FromPool(InitialBufferSize, s_clientSocketPool, forBuffer: true);
        var webSocket = _webSocket ?? throw new ObjectDisposedException(nameof(CachrWebSocketPeer));
        try
        {
            while (webSocket.CloseStatus == null)
            {
                var receiveResult =
                    await webSocket.ReceiveAsync(buffer.ArraySegment[receivedBytes..], cancellationToken);

                receivedBytes += receiveResult.Count;
                _logger.LogDebug("Received {socketReceiveCount} bytes this pass, and {totalReceiveCount} bytes total"
                , receiveResult.Count, receivedBytes);

                if (!receiveResult.EndOfMessage)
                {
                    if (receivedBytes >= buffer.Length - 1536)
                    {
                        _logger.LogDebug("Buffer is too small, doubling size.");
                        buffer.Resize(buffer.Length * 2, forBuffer: true);
                    }

                    _logger.LogDebug("EndOfMessage flag is not set, the payload is not complete, reading next fragment.");

                    continue;
                }

                if (await TryConfirmClientCloseRequestAsync(webSocket, cancellationToken))
                {
                    break;
                }

                if (receivedBytes == 0) continue;

                buffer.Resize(receivedBytes);
                if (!Enabled)
                {
                    receivedBytes = 0;
                    continue;
                }

                var resultBuffer = buffer.Clone(useDefaultPool: true);
                receivedBytes = 0;
                buffer.Resize(InitialBufferSize, forBuffer: true);
            }

            var source = new CancellationTokenSource();
            _logger.LogInformation("Web socket communication ended by peer.");
            source.CancelAfter(TimeSpan.FromSeconds(5));
            await TryConfirmClientCloseRequestAsync(webSocket, source.Token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "An exception occurred while reading data from our peer. The web socket will now be closed.");
        }
        finally
        {
            if (webSocket.State != WebSocketState.Closed)
            {
                _logger.LogWarning("WebSocket loop ended, but the connection was left open. The connection will be aborted.");
            }
            _logger.LogInformation("Web socket receive loop ended");
            buffer.Dispose();
        }
    }




    public async ValueTask SendAsync(RentedArray<byte> peerMessageData, CancellationToken cancellationToken)
    {
        try
        {
            using var _ = await _sendSemaphore.AcquireAsync(cancellationToken);
            var webSocket = _webSocket;
            if (webSocket is null) return;
            await webSocket.SendAsync(peerMessageData.ReadOnlyMemory,
                WebSocketMessageType.Binary,
                true,
                cancellationToken);
        }
        catch (ObjectDisposedException) { }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            await CloseAsync("Exception sending data", cancellationToken, true);
            _logger.LogWarning(ex, "Connection terminated");
        }
    }

    private static async ValueTask<bool> TryConfirmClientCloseRequestAsync(WebSocket webSocket, CancellationToken cancellationToken)
    {
        if (webSocket.State != WebSocketState.CloseReceived) return false;
        if (webSocket.CloseStatus == null) return false;
        try
        {
            await webSocket.CloseAsync(webSocket.CloseStatus.Value,
                webSocket.CloseStatusDescription,
                cancellationToken);
        }
        catch
        {
            webSocket.Abort();
        }

        return true;
    }
    public async ValueTask CloseAsync(string reason, CancellationToken cancellationToken, bool exceptional = false)
    {
        var webSocket = _webSocket;
        if (webSocket is null) return;
        using (_logger.BeginScope("{reason}", reason))
        using (_logger.BeginScope("{exceptional}", exceptional))
        {
            try
            {
                if (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.Connecting)
                {
                    _logger.LogWarning("Web socket closing");
                    if (!exceptional)
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,
                            $"Closed - {reason}",
                            cancellationToken);
                    else
                        await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError,
                            $"Internal server error - {reason}",
                            cancellationToken);
                }
                else
                {
                    await TryConfirmClientCloseRequestAsync(webSocket, cancellationToken);
                    webSocket.Abort();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Exception closing web socket, being rude and aborting instead.");
                try { webSocket.Abort(); } catch { /* ignored */ }
            }
        }
    }

    public void Dispose()
    {
        var webSocket = Interlocked.Exchange(ref _webSocket, null);
        if (webSocket == null) return;
        _sendSemaphore.Dispose();
        webSocket.Dispose();
    }
}
