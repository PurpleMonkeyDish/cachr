namespace Cachr.Core;

public class CacheBusDataReceivedEventArgs : EventArgs
{
    public CacheBusDataReceivedEventArgs(RentedArray<byte> data) => Data = data;
    public RentedArray<byte> Data { get; }
}