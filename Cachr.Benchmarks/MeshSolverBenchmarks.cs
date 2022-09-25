using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using Cachr.Core;
using Cachr.Core.Peering;

namespace Cachr.Benchmarks;

[JsonExporterAttribute.Full]
[JsonExporterAttribute.FullCompressed]
[MemoryDiagnoser]
[SuppressMessage("ReSharper", "ClassCanBeSealed.Global", Justification = "Required by BenchmarkDotNet")]
public class MeshSolverBenchmarks
{
    private Dictionary<Guid, PeerStateInformation> _peerMap = new Dictionary<Guid, PeerStateInformation>();
    private readonly IPeerMeshSolver _peerMeshSolver = new PeerMeshSolver();
    const int Count = 2048;

    [Params(0, 10, 20, 50)]
    public int DisconnectedPeers { get; set; }

    [Benchmark(OperationsPerInvoke = Count)]
    public void GetUnreachablePeersBenchmark()
    {
        _peerMeshSolver.GetUnreachablePeers(_peerMap);
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public void SolveMeshBenchmark()
    {
        _peerMeshSolver.SelectLocalPeers(_peerMap);
    }

    [GlobalSetup]
    public void Setup()
    {
        var ids = Enumerable.Range(0, Count).Select(i => i == Count - 1 ? NodeIdentity.Id : Guid.NewGuid()).ToArray();
        var peerMap = new Dictionary<Guid, PeerStateInformation>();
        var availablePeers = ids.ToImmutableHashSet();
        for (var x = 0; x < ids.Length; x++)
        {
            ImmutableHashSet<Guid> connectedPeers;

            if (x < DisconnectedPeers)
                connectedPeers = ImmutableHashSet<Guid>.Empty;
            else
                connectedPeers = Enumerable.Range(x, 5)
                    .Select(i => ids[i % ids.Length])
                    .Take(4)
                    .ToImmutableHashSet();
            peerMap[ids[x]] = new PeerStateInformation(ids[x], connectedPeers, availablePeers);
        }

        _peerMap = peerMap;
    }
}
