using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Cachr.Core.Buffers;

namespace Cachr.Core.Peering;

public sealed class PeerMeshSolver : IPeerMeshSolver
{
    public IEnumerable<Guid> SelectLocalPeers(IDictionary<Guid, PeerStateInformation> peerMap)
    {
        var unreachablePeers = GetUnreachablePeers(peerMap);

        return peerMap[NodeIdentity.Id].AvailablePeers
            .Union(peerMap[NodeIdentity.Id].ConnectedPeers)
            .OrderBy(i => unreachablePeers.Contains(i) ? 0 : 1)
            .ThenBy(i => peerMap[NodeIdentity.Id].ConnectedPeers.Contains(i) ? 0 : 1)
            .ThenBy(GetDeterministicOrderValue)
            .ThenBy(i => i)
            .Distinct()
            .Take(4)
            .ToImmutableHashSet();
    }

    private static int GetDeterministicOrderValue(Guid id) => HashCode.Combine(id, NodeIdentity.Id);
    public ISet<Guid> GetUnreachablePeers(IDictionary<Guid, PeerStateInformation> peerMap)
    {
        var unreachablePeers = new HashSet<Guid>(peerMap.Count);
        var explorationStack = new Stack<Guid>(peerMap.Count);
        var visitedPeers = new HashSet<Guid>(peerMap.Count);
        foreach (var peer in peerMap[NodeIdentity.Id].ConnectedPeers)
        {
            explorationStack.Push(peer);
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
