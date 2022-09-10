using System.Collections.Concurrent;
using System.Threading.Channels;
using Cachr.AspNetCore.Discovery;
using Cachr.Core.Buffers;
using Cachr.Core.Messages.Bus;
using Microsoft.AspNetCore.Http;

namespace Cachr.AspNetCore;

public class CachrMiddleware
{
    private readonly RequestDelegate _next;

    public CachrMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.ContainsKey("X-Cachr-Request"))
        {
            await ExecuteCachrRequest(context);
            return;
        }
        await _next.Invoke(context);
    }

    private async Task ExecuteCachrRequest(HttpContext context)
    {
        
    }
}

public class PeerHttpClient : IPeerHttpClient, IDisposable
{
    private readonly IPeerDiscovery _peerDiscovery;
    private readonly Thread _peerCommunicationThread;
    private bool _disposed = false;
    private readonly CancellationTokenSource _shutdownTokenSource = new CancellationTokenSource();

    public PeerHttpClient(IPeerDiscovery peerDiscovery)
    {
        _peerDiscovery = peerDiscovery;
        _peerCommunicationThread = new Thread(PeerCommunicationThreadCallback)
        {
            Name = "Cachr Peer Communication Thread"
        };
        _peerCommunicationThread.UnsafeStart(this);
    }

    private static void PeerCommunicationThreadCallback(object? obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        var peerClient = (PeerHttpClient) obj;
        peerClient.HandlePeersAsync().GetAwaiter().GetResult();
    }

    private Channel<(string, RentedArray<byte>)> _outboundChannel =
        Channel.CreateBounded<(string, RentedArray<byte>)>(new BoundedChannelOptions(1000)
        {
            SingleReader = true, 
            SingleWriter = false, 
            AllowSynchronousContinuations = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });

    private async Task HandlePeersAsync()
    {
        try
        {
            await foreach (var message in _outboundChannel.Reader
                               .ReadAllAsync()
                               .WithCancellation(_shutdownTokenSource.Token)
                           )
            {
                
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch(ChannelClosedException)
        {
        }
        finally
        {
            _shutdownTokenSource.Dispose();
            _outboundChannel.Writer.TryComplete();
        }
    }

    public static void ReplyTo(string peer, RentedArray<byte> rentedArray)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _shutdownTokenSource.Cancel();
            _peerCommunicationThread.Join(TimeSpan.FromSeconds(30));
        }
    }
}

public interface IPeerHttpClient
{
}

public class CachrAspNetCoreBus : ICacheBus
{
    public CachrAspNetCoreBus(IPeerDiscovery peerDiscovery)
    {
        
    }
    public void Broadcast(RentedArray<byte> payload)
    {
        throw new NotImplementedException();
    }

    public void SendToOneRandom(RentedArray<byte> payload)
    {
        throw new NotImplementedException();
    }

    public bool Ready { get; internal set; }

    internal void OnDataReceived(string peer, RentedArray<byte> data) => DataReceived?.Invoke(this,
        new CacheBusDataReceivedEventArgs(peer, data, d => PeerHttpClient.ReplyTo(peer, d)));
    public event EventHandler<CacheBusDataReceivedEventArgs>? DataReceived;
}