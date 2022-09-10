namespace Cachr.Core;

public interface ICacheBus
{
    void Broadcast(byte[] payload);
    void SendToOneRandom(byte[] payload);
    event EventHandler<CacheBusDataReceivedEventArgs> DataReceived;
}