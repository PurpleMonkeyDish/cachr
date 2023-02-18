using Orleans;

namespace Cachr.Core;

public interface IBlobGrain : IGrainWithStringKey
{
    Task<byte[]?> GetAsync(GrainCancellationToken cancellationToken);
    Task SetAsync(byte[] data, TimeSpan timeToLive, GrainCancellationToken cancellationToken);
    Task UpdateTimeToLiveAsync(TimeSpan timeToLive, GrainCancellationToken cancellationToken);
    Task DeleteAsync(GrainCancellationToken cancellationToken);
}
