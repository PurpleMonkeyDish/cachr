using System.Collections.Immutable;
using Cachr.Core.Discovery;
using Cachr.Core.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cachr.Core.Peering;

public sealed class GossipTransportManagerService : BackgroundService
{
    private readonly IPeerStatusTracker _peerStatusTracker;
    private readonly IPeerSelector _peerSelector;
    private readonly IGossipTransportProvider _gossipTransportProvider;
    private readonly IMessageBus<PeerStateUpdateMessage> _peerStateBus;
    private readonly ILogger<GossipTransportManagerService> _logger;
    private readonly Dictionary<Guid, IGossipTransport> _transports = new Dictionary<Guid, IGossipTransport>();

    public GossipTransportManagerService(
        IPeerStatusTracker peerStatusTracker,
        IPeerSelector peerSelector,
        IGossipTransportProvider gossipTransportProvider,
        IMessageBus<PeerStateUpdateMessage> peerStateBus,
        ILogger<GossipTransportManagerService> logger
    )
    {
        _peerStatusTracker = peerStatusTracker;
        _peerSelector = peerSelector;
        _gossipTransportProvider = gossipTransportProvider;
        _peerStateBus = peerStateBus;
        _logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(29));
            var tasks = new List<Task>();
            while (true)
            {
                tasks.Insert(0, periodicTimer.WaitForNextTickAsync(stoppingToken).AsTask());
                do
                {
                    var completedTask = await Task.WhenAny(tasks).ConfigureAwait(false);
                    if (ReferenceEquals(completedTask, tasks[0]))
                    {
                        tasks.RemoveAt(0);
                        break;
                    }
                    tasks.Remove(completedTask);

                } while (true);

                while (tasks.Count > 10)
                {
                    var completedTask = await Task.WhenAny(tasks);
                    tasks.Remove(completedTask);
                }

                var peerStates = _peerStatusTracker.GetAllUpdates();
                var connectedPeers = _transports.Keys.ToArray();
                var selectedPeers = _peerSelector
                    .SelectPeers(peerStates.Where(i => i.Peer.Id != NodeIdentity.Id), connectedPeers)
                    .DistinctBy(i => i.Id)
                    .ToDictionary(i => i.Id, i => i);
                var selectedPeerIds = selectedPeers.Keys
                    .ToArray();



                foreach (var peerId in connectedPeers.Except(selectedPeerIds).Distinct())
                {
                    _transports.Remove(peerId, out var removed);
                    if (removed is null) continue;
                    _logger.LogInformation("Disconnecting peer {id}", peerId);
                    tasks.Add(removed.DisconnectAsync(stoppingToken));
                }

                var newPeerTasks = new List<Task<IGossipTransport?>>();
                foreach (var peerId in selectedPeerIds.Except(connectedPeers).Distinct())
                {
                    var peer = selectedPeers[peerId];
                    newPeerTasks.Add(_gossipTransportProvider.TryConnectToPeerAsync(peer, stoppingToken));
                }

                if (newPeerTasks.Count <= 0)
                    continue;

                var newPeerResults = await Task.WhenAll(newPeerTasks);
                foreach (var peer in newPeerResults.Where(i => i is not null).Cast<IGossipTransport>())
                {
                    if (!_transports.TryAdd(peer.Peer.Id, peer))
                    {
                        tasks.Add(peer.DisconnectAsync(stoppingToken));
                        continue;
                    }

                    tasks.Add(_peerStateBus.BroadcastAsync(new PeerStateUpdateMessage(
                        Guid.NewGuid(),
                        PeerState.Connected,
                        peer.Peer,
                        ImmutableArray<Peer>.Empty,
                        DateTimeOffset.Now.ToUnixTimeMilliseconds()
                    ), stoppingToken));

                    _logger.LogInformation("Established gossip channel with {id}", peer.Peer.Id);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {

        }
        finally
        {
            _logger.LogInformation("Broadcasting shutdown message to internal message bus");
            // ReSharper disable MethodSupportsCancellation
            // We don't want cancellation here, .NET will nuke us if we take too long.
            await _peerStateBus.BroadcastAsync(new PeerStateUpdateMessage(
                Guid.NewGuid(),
                PeerState.ShuttingDown,
                Peer.CreateSelf(Enumerable.Empty<string>(), "default"),
                ImmutableArray<Peer>.Empty,
                DateTimeOffset.Now.ToUnixTimeMilliseconds()));
            // Give the message time to make it through the queues.
            await Task.Delay(TimeSpan.FromSeconds(1));
            // ReSharper restore MethodSupportsCancellation
            _logger.LogInformation("Shutdown completed successfully");
        }
    }
}