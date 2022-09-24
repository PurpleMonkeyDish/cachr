using System.Runtime.CompilerServices;
using Cachr.Core.Buffers;

namespace Cachr.AspNetCore;

public sealed record PeerReceivedMessageData(Guid Id, Guid SenderId, Guid? TargetId, RentedArray<byte> Data)
    : PeerMessageDataBase(Id, SenderId, TargetId, Data)
{
    private readonly TaskCompletionSource _taskCompletionSource = new();
    public Task Completed => _taskCompletionSource.Task;
    public TaskAwaiter GetAwaiter() => Completed.GetAwaiter();

    public override void Dispose()
    {
        _taskCompletionSource.TrySetResult();
        base.Dispose();
    }
}