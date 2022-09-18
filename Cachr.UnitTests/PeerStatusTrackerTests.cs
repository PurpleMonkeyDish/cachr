using System;
using System.Collections.Immutable;
using System.Linq;
using Cachr.Core.Discovery;
using Cachr.Core.Peering;
using Xunit;

namespace Cachr.UnitTests;

public sealed class PeerStatusTrackerTests
{
    private static IPeerStatusTracker CreateStatusTracker()
    {
        return new PeerStatusTracker();
    }
    [Fact]
    public void TrackerRemovesDeadPeersFromBothConnectedAndKnownWhenMessageIsNewer()
    {
        var tracker = CreateStatusTracker();
        var fakePeer = new Peer(Guid.NewGuid(), new[] {"127.0.0.1:5001"}.ToImmutableArray(), "unknown");
        var expectedPeerStatusUpdateMessage = PeerStateUpdateMessage.Create(PeerState.Full, fakePeer,  new[]
        {
            fakePeer with { Id = Guid.NewGuid() },
            fakePeer with { Id = Guid.NewGuid() },
            fakePeer with { Id = Guid.NewGuid() },
            fakePeer with { Id = Guid.NewGuid(), EndPoints = Enumerable.Range(0, 100).Select(x => x.ToString()).ToImmutableArray() }
        });

        tracker.NotifyPeerStateUpdate(expectedPeerStatusUpdateMessage);
        tracker.NotifyNewConnection(expectedPeerStatusUpdateMessage.Peer.Id);

        var connectedPeer = Assert.Single(tracker.GetConnectedPeers());
        var peerStatusUpdateMessage = Assert.Single(tracker.GetAllUpdates());

        Assert.Equal(expectedPeerStatusUpdateMessage.Id, peerStatusUpdateMessage.Id);
        Assert.Equal(connectedPeer, expectedPeerStatusUpdateMessage.Peer.Id);
        expectedPeerStatusUpdateMessage = expectedPeerStatusUpdateMessage with
        {
            State = PeerState.ShuttingDown,
            Connections = ImmutableArray<Peer>.Empty,
            Id = Guid.NewGuid(),
            TimeStamp = expectedPeerStatusUpdateMessage.TimeStamp - 100
        };

        tracker.NotifyConnectionLost(connectedPeer);
        Assert.Empty(tracker.GetConnectedPeers());
        peerStatusUpdateMessage = Assert.Single(tracker.GetAllUpdates());
        tracker.NotifyNewConnection(connectedPeer);
        tracker.NotifyNewConnection(connectedPeer);

        connectedPeer = Assert.Single(tracker.GetConnectedPeers());

        Assert.Equal(connectedPeer, expectedPeerStatusUpdateMessage.Peer.Id);

        tracker.NotifyPeerStateUpdate(expectedPeerStatusUpdateMessage);

        connectedPeer = Assert.Single(tracker.GetConnectedPeers());
        peerStatusUpdateMessage = Assert.Single(tracker.GetAllUpdates());

        tracker.NotifyPeerStateUpdate(expectedPeerStatusUpdateMessage with { TimeStamp = expectedPeerStatusUpdateMessage.TimeStamp + 5000 });

        Assert.Empty(tracker.GetConnectedPeers());
        Assert.Empty(tracker.GetAllUpdates());
    }
}
