namespace Cachr.Core.Peering;

public interface IPeerManager
{
    bool AddPeer(IPeerConnection peerConnection);
    void RemovePeer(IPeerConnection peerConnection);
}
