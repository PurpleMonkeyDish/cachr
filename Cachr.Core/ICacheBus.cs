namespace Cachr.Core;

public interface ICacheBus
{
    void Broadcast(byte[] payload);
    Task BroadcastAsync(byte[] payload, CancellationToken cancellationToken);
    IEnumerable<string> GetKnownHosts();
    event EventHandler<CacheBusDataReceivedEventArgs> DataReceived;
}