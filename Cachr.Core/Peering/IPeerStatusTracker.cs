using Cachr.Core.Discovery;

namespace Cachr.Core.Peering;

public interface IPeerStatusTracker
{
    IEnumerable<PeerStateUpdateMessage> GetAllUpdates();
    IEnumerable<Guid> GetConnectedPeers();
    void NotifyPeerStateUpdate(PeerStateUpdateMessage message);
    void NotifyNewConnection(Guid id);
    void NotifyConnectionLost(Guid id);
}
