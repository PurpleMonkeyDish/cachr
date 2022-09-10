using Cachr.Core.Buffers;

namespace Cachr.Core.Messages.Bus;

public interface ICacheBus
{
    void Broadcast(RentedArray<byte> payload);
    void SendToOneRandom(RentedArray<byte> payload);
    bool Ready { get; }
    event EventHandler<CacheBusDataReceivedEventArgs> DataReceived;
}