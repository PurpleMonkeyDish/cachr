using System.Reflection.Metadata;
using System.Threading.Channels;
using Cachr.Core.Discovery;
using Cachr.Core.Messaging;
using Microsoft.Extensions.Hosting;

namespace Cachr.Core.Peering;

public class PeerConnectionManager : BackgroundService
{

    private readonly object _peerConnectionsLock = new object();

    private enum UpdateType
    {
        Add,
        Remove
    }
    private readonly Channel<(UpdateType, IPeerConnection)> _connectionStateChangedNotificationChannel =
        Channel.CreateBounded<(UpdateType, IPeerConnection)>(new BoundedChannelOptions(100)
        {
            SingleReader = true, SingleWriter = false
        });

    public async Task AddPeer(IPeerConnection peerConnection)
    {
        await _connectionStateChangedNotificationChannel.Writer.WriteAsync((UpdateType.Add, peerConnection));
    }

    public async Task RemovePeer(IPeerConnection peerConnection)
    {
        await _connectionStateChangedNotificationChannel.Writer.WriteAsync((UpdateType.Remove, peerConnection));
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }

}
