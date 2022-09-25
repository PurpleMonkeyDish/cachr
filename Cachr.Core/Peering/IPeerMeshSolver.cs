namespace Cachr.Core.Peering;

public interface IPeerMeshSolver
{
    IEnumerable<Guid> SelectLocalPeers(IDictionary<Guid, PeerStateInformation> peerMap);

    ISet<Guid> GetUnreachablePeers(IDictionary<Guid, PeerStateInformation> peerMap);
}