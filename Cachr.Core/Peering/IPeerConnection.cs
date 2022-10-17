using Cachr.Core.Buffers;

namespace Cachr.Core.Peering;

public interface IPeerConnection
{
    bool Enabled { get; set; }
    PeerDescription Description { get; }
    ValueTask SendAsync(RentedArray<byte> peerMessageData, CancellationToken cancellationToken);
    ValueTask CloseAsync(string reason, CancellationToken cancellationToken, bool exceptional = false);
}
