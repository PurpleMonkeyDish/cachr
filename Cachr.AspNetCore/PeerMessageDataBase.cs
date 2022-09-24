using Cachr.Core.Buffers;

namespace Cachr.AspNetCore;

public abstract record PeerMessageDataBase(Guid Id, Guid SenderId, Guid? TargetId, RentedArray<byte> Data) : IDisposable
{
    public virtual void Dispose()
    {
        Data.Dispose();
    }
}
