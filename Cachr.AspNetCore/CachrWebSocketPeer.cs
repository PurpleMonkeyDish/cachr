using System.Diagnostics;
using System.IO.Compression;
using System.Net.WebSockets;
using System.Text.Json;
using Cachr.Core.Buffers;
using Cachr.Core.Messages.Duplication;
using Cachr.Core.Messaging;

namespace Cachr.AspNetCore;

public sealed class CachrWebSocketPeer : IAsyncDisposable
{
    private readonly Guid _id;
    private readonly WebSocket _webSocket;
    private readonly IMessageBus<PeerReceivedMessageData> _peerDataReceivedMessageBus;
    private readonly IMessageBus<PeerMessageData> _peerDataMessageBus;

    private readonly IDuplicateTracker<Guid> _duplicateTracker =
        new DuplicateTracker<Guid>(32, 10000, TimeSpan.FromMinutes(2));


    public CachrWebSocketPeer(Guid id, WebSocket webSocket, IMessageBus<PeerMessageData> peerDataMessageBus,
        IMessageBus<PeerReceivedMessageData> peerDataReceivedMessageBus)
    {
        _id = id;
        _webSocket = webSocket;
        _peerDataReceivedMessageBus = peerDataReceivedMessageBus;
        _peerDataMessageBus = peerDataMessageBus;
    }

    public async Task RunPeerAsync(CancellationToken cancellationToken)
    {
        using var source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var targetedSubscriptionToken =
            _peerDataMessageBus.Subscribe(m => OnTargetedMessageReceived(m, cancellationToken), SubscriptionMode.All);
        await using var _ = targetedSubscriptionToken.ConfigureAwait(false);
        await HandleReadsAsync(cancellationToken).ConfigureAwait(false);
        source.Cancel();
    }

    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    private async ValueTask OnTargetedMessageReceived(PeerMessageData message, CancellationToken cancellationToken)
    {
        if (message.TargetId is not null && message.TargetId != _id) return; // Ignored targeted not aimed at us.
        if (_duplicateTracker.IsDuplicate(message.Id)) return; // Drop duplicates.
        await OnMessageReceived(message, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask OnMessageReceived(PeerMessageData message, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_webSocket.CloseStatus != null) return;
            using var serializedMessage = SerializeMessage(message);
            await _webSocket.SendAsync(
                serializedMessage.ArraySegment,
                WebSocketMessageType.Binary,
                true,
                cancellationToken
            ).ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private RentedArray<byte> SerializeMessage<T>(T objectToSerialize)
    {
        using var memoryStream = new MemoryStream();
        using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress, leaveOpen: true))
        {
            JsonSerializer.Serialize(gzipStream, objectToSerialize);
            gzipStream.Flush();
        }

        var rentedArray =
            RentedArray<byte>.FromDefaultPool((int)memoryStream.Length + ArraySegmentExtensions.GzipSentinel.Length);
        using var rentedArrayStream = rentedArray.ToMemoryStream(writable: true);
        rentedArrayStream.Write(ArraySegmentExtensions.GzipSentinel);
        memoryStream.Seek(0, SeekOrigin.Begin);

        memoryStream.CopyTo(rentedArrayStream);

        return rentedArray;
    }

    private async Task HandleReadsAsync(CancellationToken cancellationToken)
    {
        RentedArray<byte>? messageBuffer = null;
        WebSocketReceiveResult? receiveResult = null;
        while (receiveResult?.CloseStatus == null)
        {
            using var localBuffer = RentedArray<byte>.FromDefaultPool(4096);
            receiveResult = await _webSocket.ReceiveAsync(localBuffer.ArraySegment, cancellationToken).ConfigureAwait(false);

            if (receiveResult.CloseStatus != null) continue;

            var previousBuffer = messageBuffer ?? RentedArray<byte>.Empty;

            messageBuffer = RentedArray<byte>.FromDefaultPool(receiveResult.Count + previousBuffer.Length);
            if (previousBuffer.Length > 0)
            {
                previousBuffer.ArraySegment.CopyTo(messageBuffer.ArraySegment);
            }

            localBuffer.ArraySegment[..receiveResult.Count]
                .CopyTo(messageBuffer.ArraySegment[previousBuffer.Length..receiveResult.Count]);

            previousBuffer.Dispose();

            if (!receiveResult.EndOfMessage)
                continue;

            previousBuffer = messageBuffer;
            await TryDecodeAndPublishMessage(previousBuffer, cancellationToken).ConfigureAwait(false);
            messageBuffer = null;
        }

        await _webSocket.CloseAsync(receiveResult.CloseStatus.Value, receiveResult.CloseStatusDescription,
            cancellationToken).ConfigureAwait(false);
    }

    private static T? DeserializePayload<T>(RentedArray<byte> rentedArray, bool disposeWhenDone = true)
        where T : class
    {
        using (disposeWhenDone ? rentedArray : null)
        {
            var arraySegment = rentedArray.ArraySegment;
            Debug.Assert(arraySegment.Array is not null);
            using var stream = arraySegment.DetectAndWrapWithDecompressionStream();

            var result = JsonSerializer.Deserialize<T>(stream);

            return result;
        }
    }


    private async ValueTask TryDecodeAndPublishMessage(RentedArray<byte> buffer, CancellationToken cancellationToken)
    {
        using var peerMessage = DeserializePayload<PeerReceivedMessageData>(buffer);

        if (peerMessage is null) // What?
            return;
        if (_duplicateTracker.IsDuplicate(peerMessage.Id))
            return;

        await _peerDataReceivedMessageBus.BroadcastAsync(peerMessage, cancellationToken).ConfigureAwait(false);

        await peerMessage; // Wait for the message to be handled.
    }

    public ValueTask DisposeAsync()
    {
        _webSocket.Dispose();
        return ValueTask.CompletedTask;
    }
}