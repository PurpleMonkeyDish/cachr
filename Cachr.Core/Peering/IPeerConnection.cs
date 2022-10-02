using Cachr.Core.Buffers;

namespace Cachr.Core.Peering;

public interface IPeerConnection
{
    Guid Id { get; }
    bool Enabled { get; set; }
    Uri Uri { get; }
    ValueTask SendAsync(RentedArray<byte> peerMessageData, CancellationToken cancellationToken);
    ValueTask CloseAsync();
}
