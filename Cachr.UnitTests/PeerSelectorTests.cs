using System;
using System.Collections.Immutable;
using System.Linq;
using Cachr.Core;
using Cachr.Core.Discovery;
using Cachr.Core.Peering;
using Xunit;

namespace Cachr.UnitTests;

public sealed class PeerSelectorTests
{
    static PeerSelectorTests()
    {
        s_fakePeer = new Peer(Guid.NewGuid(), new[] {"127.0.0.1:5001"}.ToImmutableArray(), "unknown");
        s_knownPeer = PeerStateUpdateMessage.Create(PeerState.Known, s_fakePeer, ImmutableArray<Peer>.Empty);
        s_self = PeerStateUpdateMessage.Create(PeerState.Known, s_fakePeer with { Id = NodeIdentity.Id }, ImmutableArray<Peer>.Empty);
    }

    private static readonly Peer s_fakePeer;
    private static readonly PeerStateUpdateMessage s_knownPeer;
    private static readonly PeerStateUpdateMessage s_self;

    private IPeerSelector CreatePeerSelector() => new PeerSelector();
    [Fact]
    public void PeerSelectorReturnsEmptyWhenGivenEmpty()
    {
        var selector = CreatePeerSelector();

        var result = selector.SelectPeers(Enumerable.Empty<PeerStateUpdateMessage>(), Enumerable.Empty<Guid>());

        Assert.Empty(result);
    }

    [Fact]
    public void PeerSelectorReachableReturnsFalseWhenEmpty()
    {
        var selector = CreatePeerSelector();
        var result = selector.AllPeersReachable(Enumerable.Empty<PeerStateUpdateMessage>(), Enumerable.Empty<Guid>());
        Assert.False(result);
    }

    [Fact]
    public void MeshIsCompleteWhenOnlyOnePeerAndPeerIsConnected()
    {
        var selector = CreatePeerSelector();
        var connections = new[] {s_knownPeer.Peer.Id};
        var result = selector.AllPeersReachable(new[] {s_knownPeer }, Enumerable.Empty<Guid>());

        Assert.False(result);
        result = selector.AllPeersReachable(new[] {s_knownPeer}, connections);
        Assert.True(result);
    }

    [Fact]
    public void MeshIsCompleteWhenPeersHaveConnectionsToEachOther()
    {
        var meshPeers = new[]
        {
            s_knownPeer,
            s_knownPeer with {Id = Guid.NewGuid(), Peer = s_knownPeer.Peer with {Id = Guid.NewGuid()}},
            s_knownPeer with {Id = Guid.NewGuid(), Peer = s_knownPeer.Peer with {Id = Guid.NewGuid()}}
        };

        for (var x = 0; x < meshPeers.Length; x++)
        {
            meshPeers[x] = meshPeers[x] with
            {
                Connections =
                meshPeers.Where(i => i.Id != meshPeers[x].Id).Select(i => i.Peer).ToImmutableArray()
            };
        }
        var selector = CreatePeerSelector();
        Assert.True(selector.AllPeersReachable(meshPeers, Enumerable.Empty<Guid>()));
    }

    [Fact]
    public void MeshReturnsAllPeersWhenPeerCountIsLessThanThree()
    {
        var meshPeers = new[]
        {
            s_knownPeer,
            s_knownPeer with {Id = Guid.NewGuid(), Peer = s_knownPeer.Peer with {Id = Guid.NewGuid()}},
            s_knownPeer with {Id = Guid.NewGuid(), Peer = s_knownPeer.Peer with {Id = Guid.NewGuid()}}
        };

        for (var x = 0; x < meshPeers.Length; x++)
        {
            meshPeers[x] = meshPeers[x] with
            {
                Connections =
                meshPeers.Where(i => i.Id != meshPeers[x].Id).Select(i => i.Peer).ToImmutableArray()
            };
        }

        var selector = CreatePeerSelector();

        var results = selector.SelectPeers(meshPeers, Enumerable.Empty<Guid>()).ToArray();

        Assert.NotEmpty(results);
        Assert.Distinct(results);
        var count = results.Length;
        Assert.Equal(meshPeers.Length, count);
    }
}
