using Cachr.Core.Buffers;

namespace Cachr.Core.Messages.Bus;

public sealed class LocalOnlyCacheBus : ICacheBus
{
    public void Broadcast(RentedArray<byte> payload)
    {
        payload.Dispose();
    }

    public void SendToOneRandom(RentedArray<byte> payload)
    {
        payload.Dispose();
    }

    public bool Ready => true;

    public event EventHandler<CacheBusDataReceivedEventArgs>? DataReceived;
}