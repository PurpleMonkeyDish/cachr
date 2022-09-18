using Cachr.Core.Discovery;

namespace Cachr.Core.Peering;

public interface IPeerSelector
{
    bool AllPeersReachable(IEnumerable<PeerStateUpdateMessage> peers, IEnumerable<Guid> connectedPeers);
    IEnumerable<Peer> SelectPeers(IEnumerable<PeerStateUpdateMessage> peers, IEnumerable<Guid> connectedPeers);
}
