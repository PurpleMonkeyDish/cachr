using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Cachr.Core;
using Cachr.Core.Peering;
using Xunit;

namespace Cachr.UnitTests;

public sealed class MeshSolverTests
{
    private IPeerMeshSolver _meshSolver = new PeerMeshSolver();

    [Fact]
    public void MeshSolverDetectsPartitions()
    {
        const int PartitionSize = 16;
        var ids = Enumerable.Range(0, PartitionSize * 2).Select(i => i == 0 ? NodeIdentity.Id : Guid.NewGuid()).ToArray();
        var partitionA = ids[..PartitionSize];
        var partitionB = ids[PartitionSize..];
        var peerMap = new Dictionary<Guid, PeerStateInformation>();
        var availablePeers = ids.ToImmutableHashSet();
        for (var x = 0; x < partitionA.Length; x++)
        {
            var connectedPeers = Enumerable.Range(x, 4)
                .Select(i => partitionA[i % partitionA.Length])
                .ToImmutableHashSet();
            peerMap[partitionA[x]] = new PeerStateInformation(partitionA[x], connectedPeers, availablePeers);
            connectedPeers = Enumerable.Range(x, 4)
                .Select(i => partitionB[i % partitionB.Length])
                .ToImmutableHashSet();
            peerMap[partitionB[x]] = new PeerStateInformation(partitionB[x], connectedPeers, availablePeers);
        }


        var unreachablePeers = _meshSolver.GetUnreachablePeers(peerMap);

        Assert.NotEmpty(unreachablePeers);
        Assert.Equal(partitionB.OrderBy(i => i), unreachablePeers.OrderBy(i => i));
    }

    [Fact]
    public void MeshSolverDetectsSingleUnreachableNodes()
    {
        const int PartitionSize = 16;
        var ids = Enumerable.Range(0, PartitionSize * 2).Select(i => i == 0 ? NodeIdentity.Id : Guid.NewGuid()).ToArray();
        var peerMap = new Dictionary<Guid, PeerStateInformation>();
        var availablePeers = ids.ToImmutableHashSet();
        const int ExpectedMissingIndex = 1;
        for (var x = 0; x < ids.Length; x++)
        {
            var connectedPeers = Enumerable.Range(x, 5)
                .Select(i => ids[i % ids.Length])
                .Where(i => i != ids[ExpectedMissingIndex])
                .Take(4)
                .ToImmutableHashSet();
            peerMap[ids[x]] = new PeerStateInformation(ids[x], connectedPeers, availablePeers);
        }

        var unreachablePeers = _meshSolver.GetUnreachablePeers(peerMap);

        var disconnectedPeer = Assert.Single(unreachablePeers);
        Assert.Equal(ids[ExpectedMissingIndex], disconnectedPeer);
    }

    [Fact]
    public void MeshSolverReturnsPeersThatAreDisconnectedWhenSolvingMeshWithDisconnectedPeer()
    {
        const int PartitionSize = 16;
        var ids = Enumerable.Range(0, PartitionSize * 2).Select(i => i == 0 ? NodeIdentity.Id : Guid.NewGuid()).ToArray();
        var peerMap = new Dictionary<Guid, PeerStateInformation>();
        var availablePeers = ids.ToImmutableHashSet();
        const int ExpectedMissingIndex = 1;
        for (var x = 0; x < ids.Length; x++)
        {
            var connectedPeers = Enumerable.Range(x, 5)
                .Select(i => ids[i % ids.Length])
                .Where(i => i != ids[ExpectedMissingIndex])
                .Take(4)
                .ToImmutableHashSet();
            peerMap[ids[x]] = new PeerStateInformation(ids[x], connectedPeers, availablePeers);
        }


        var set = _meshSolver.SelectLocalPeers(peerMap).ToImmutableHashSet();
        Assert.NotEmpty(set);
        Assert.Contains(ids[ExpectedMissingIndex], set);
        Assert.NotEqual(peerMap[NodeIdentity.Id].ConnectedPeers.OrderBy(i => i), set.OrderBy(i => i));

        peerMap[NodeIdentity.Id] = peerMap[NodeIdentity.Id] with { ConnectedPeers = set.ToImmutableHashSet() };

        set = _meshSolver.SelectLocalPeers(peerMap).ToImmutableHashSet();

        Assert.NotEmpty(set);
        Assert.Contains(ids[ExpectedMissingIndex], set);
        Assert.Equal(peerMap[NodeIdentity.Id].ConnectedPeers.OrderBy(i => i), set.OrderBy(i => i));
    }
}
