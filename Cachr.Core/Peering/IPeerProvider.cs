namespace Cachr.Core.Peering;

public interface IPeerProvider
{
    Task<IPeerConnection> GetOrEstablishPeerConnection(PeerDescription peerDescription, CancellationToken cancellationToken);
}