using Cachr.Core.Buffers;
using Cachr.Core.Messages.Encoder;

namespace Cachr.Core.Messages.Bus;

public class CacheBusDataReceivedEventArgs : EventArgs, IDisposable
{
    private readonly Action<RentedArray<byte>> _replyCallback;

    public CacheBusDataReceivedEventArgs(string peer, RentedArray<byte> data, Action<RentedArray<byte>> replyCallback)
    {
        ArgumentNullException.ThrowIfNull(peer);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(replyCallback);
        _replyCallback = replyCallback;
        Peer = peer;
        Data = data;
    }

    public string Peer { get; }
    public RentedArray<byte> Data { get; }

    public IDistributedCacheMessage Decode() => DistributedCacheMessageEncoder.Decode(Data);
    public void Reply(RentedArray<byte> array) => _replyCallback.Invoke(array);

    public void Dispose()
    {
        Data.Dispose();
    }
}