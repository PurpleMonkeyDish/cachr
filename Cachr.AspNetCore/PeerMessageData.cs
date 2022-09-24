using Cachr.Core.Buffers;

namespace Cachr.AspNetCore;

public sealed record PeerMessageData(Guid Id, Guid SenderId, Guid? TargetId, RentedArray<byte> Data) 
    : PeerMessageDataBase(Id, SenderId, TargetId, Data);
