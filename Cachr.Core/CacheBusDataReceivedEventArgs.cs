namespace Cachr.Core;

public class CacheBusDataReceivedEventArgs : EventArgs
{
    public CacheBusDataReceivedEventArgs(string peer, RentedArray<byte> data)
    {
        Peer = peer;
        Data = data;
    }

    public string Peer { get; }
    public RentedArray<byte> Data { get; }
}