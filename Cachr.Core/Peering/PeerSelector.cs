using Cachr.Core.Discovery;

namespace Cachr.Core.Peering;

public sealed class PeerSelector : IPeerSelector
{
    // This class should be made configurable in the near future.
    public bool AllPeersReachable(IEnumerable<PeerStateUpdateMessage> peers, IEnumerable<Guid> connectedPeers)
        => IsMeshComplete(peers, connectedPeers);

    public IEnumerable<Peer> SelectPeers(IEnumerable<PeerStateUpdateMessage> peers, IEnumerable<Guid> connectedPeers)
    {
        // Eliminate non-connectable peers
        // Suspect is a connetable state, because it may not be down, and we may be able to pull it back into the mesh.
        var peerArray = peers.Where(i => i.State <= PeerState.Suspect).ToArray();
        // Full mesh, until we reach 4+ nodes.
        // But never more than 10 connections.
        var targetConnectionCount = Math.Min(Math.Max(3, (int)(peerArray.Length / 3)), 10);

        if (peerArray.Length <= targetConnectionCount) return peerArray.Select(i => i.Peer);
        var peerIdSet = peerArray
            // Don't bother trying to connect to peers that are not reliable.
            // If they become reliable again, they'll make new outbound connections on their own.
            .Where(i => i.State < PeerState.Suspect)
            .Select(i => i.Peer.Id)
            .Where(i => i != NodeIdentity.Id)
            .ToHashSet();
        var connectedPeerIdSet = connectedPeers.ToHashSet();
        if (peerIdSet.Count < targetConnectionCount)
            return peerArray.Where(i => peerIdSet.Contains(i.Peer.Id)).Select(i => i.Peer);
        var completeMesh = IsMeshComplete(peerArray);
        var targetPeers = SelectNewPeers(peerArray, connectedPeerIdSet)
            .Take(targetConnectionCount)
            .ToArray();

        if (!completeMesh)
        {
            // The mesh isn't complete.

            // Do we complete the mesh?
            if (IsMeshComplete(peerArray, targetPeers.Select(x => x.Peer.Id)))
                // If so, then we're good to go!
                return targetPeers.Select(x => x.Peer);

            // Would we complete the mesh if we didn't consider our already connected peers?
            var newTargetPeers = SelectNewPeers(peerArray)
                .Take(targetConnectionCount)
                .ToArray();

            if (IsMeshComplete(peerArray, newTargetPeers.Select(x => x.Peer.Id)))
                // If we do, return those connections instead.
                return newTargetPeers.Select(x => x.Peer);
        }

        // Either the mesh is complete, or we can't solve the mesh.
        // So just return

        return targetPeers.Select(i => i.Peer);
    }

    private static bool IsMeshComplete(IEnumerable<PeerStateUpdateMessage> peerStateUpdateMessages,
        IEnumerable<Guid>? connectedPeers = null)
    {
        connectedPeers ??= Enumerable.Empty<Guid>();
        var allPeersArray = peerStateUpdateMessages.ToArray();
        var peerSet = allPeersArray.SelectMany(i => i.Connections)
            .Select(i => i.Id)
            .Union(connectedPeers)
            .Distinct()
            .ToHashSet();

        return allPeersArray.All(x => peerSet.Contains(x.Peer.Id));
    }

    private static IEnumerable<PeerStateUpdateMessage> SelectNewPeers(
        IEnumerable<PeerStateUpdateMessage> peerStateUpdateMessages,
        IReadOnlySet<Guid>? connectedPeers = null
    )
    {
        var peerStateUpdateArray = peerStateUpdateMessages.ToArray();
        var connectionCountDictionary = peerStateUpdateArray.SelectMany(i => i.Connections)
            .GroupBy(i => i.Id)
            .ToDictionary(i => i.Key, i => i.Count());
        return peerStateUpdateArray
            // We want to stay connected to our peers
            .OrderBy(i => connectedPeers?.Contains(i.Peer.Id) == true ? 0 : 1)
            // Peers with the least global connections
            .ThenBy(i => connectionCountDictionary[i.Peer.Id])
            // Then we want peers with the fewest connections
            .ThenBy(i => i.Connections.Length)
            // Then we prefer Known, followed by peers connected to our peer
            .ThenBy(i => i.State)
            // but we want a random order for each
            .ThenBy(i => Random.Shared.NextDouble());
    }
}
