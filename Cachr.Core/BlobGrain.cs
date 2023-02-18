using Orleans;

namespace Cachr.Core;

public class BlobGrain : Grain, IBlobGrain
{
    private byte[]? _data;

    public Task<byte[]?> GetAsync(GrainCancellationToken cancellationToken)
    {
        cancellationToken.CancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_data);
    }

    public async Task SetAsync(byte[] data, TimeSpan timeToLive, GrainCancellationToken cancellationToken)
    {
        cancellationToken.CancellationToken.ThrowIfCancellationRequested();
        _data = data;
        await UpdateTimeToLiveAsync(timeToLive, cancellationToken).ConfigureAwait(false);
    }

    public Task UpdateTimeToLiveAsync(TimeSpan timeToLive, GrainCancellationToken cancellationToken)
    {
        cancellationToken.CancellationToken.ThrowIfCancellationRequested();
        this.DelayDeactivation(timeToLive);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(GrainCancellationToken cancellationToken)
    {
        this.DeactivateOnIdle();
        cancellationToken.CancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
