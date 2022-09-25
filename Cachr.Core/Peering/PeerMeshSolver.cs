using System.Collections.Immutable;
using Cachr.Core.Buffers;

namespace Cachr.Core.Peering;

public sealed class PeerMeshSolver : IPeerMeshSolver
{
    public IEnumerable<Guid> SelectLocalPeers(IDictionary<Guid, PeerStateInformation> peerMap)
    {
        var unreachablePeers = GetUnreachablePeers(peerMap);
        var self = peerMap[NodeIdentity.Id];

        var connections = new HashSet<Guid>();
        var availablePeers = new Stack<Guid>();
        var peerConnectionCounts = peerMap.Values.SelectMany(i => i.ConnectedPeers)
            .GroupBy(i => i)
            .ToDictionary(i => i.Key, i => i.Count());

        foreach (var peer in peerMap.Keys)
        {
            peerConnectionCounts.TryAdd(peer, 0);
        }

        foreach (var peer in peerMap.Keys.OrderBy(GetDeterministicOrderValue).ThenBy(i => peerConnectionCounts[i]))
        {
            if (peer == NodeIdentity.Id) continue;
            availablePeers.Push(peer);
        }

        foreach (var connection in self.ConnectedPeers)
        {
            availablePeers.Push(connection);
        }

        foreach(var unreachablePeer in unreachablePeers.OrderBy(GetDeterministicOrderValue))
        {
            if (unreachablePeer == NodeIdentity.Id) continue;
            availablePeers.Push(unreachablePeer);
        }

        while (connections.Count < 4 && availablePeers.Count > 0)
        {
            connections.Add(availablePeers.Pop());
        }

        return connections;
    }

    private static int GetDeterministicOrderValue(Guid id)
    {
        using var combinedBytes = RentedArray<byte>.FromDefaultPool(32);
        id.TryWriteBytes(combinedBytes.ArraySegment[..16]);
        NodeIdentity.Id.TryWriteBytes(combinedBytes.ArraySegment[16..]);

        var hashCode = new HashCode();
        hashCode.AddBytes(combinedBytes.ArraySegment);

        return hashCode.ToHashCode();
    }

    public ISet<Guid> GetUnreachablePeers(IDictionary<Guid, PeerStateInformation> peerMap)
    {
        var unreachablePeers = new HashSet<Guid>();
        foreach (var peer in peerMap.Keys.ToImmutableArray())
        {
            var explorationStack = new Stack<Guid>();
            explorationStack.Push(peer);
            var visitedPeers = new HashSet<Guid>();
            while (explorationStack.Count > 0)
            {
                var peerToExplore = explorationStack.Pop();
                foreach (var exploredPeer in peerMap[peerToExplore].ConnectedPeers.Where(exploredPeer => visitedPeers.Add(exploredPeer)))
                {
                    explorationStack.Push(exploredPeer);
                }
            }

            foreach (var unreachablePeer in peerMap.Keys.Except(visitedPeers))
            {
                unreachablePeers.Add(unreachablePeer);
            }
        }

        return unreachablePeers;
    }
}