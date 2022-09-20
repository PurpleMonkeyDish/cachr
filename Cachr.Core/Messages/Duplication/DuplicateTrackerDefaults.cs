namespace Cachr.Core.Messages.Duplication;

internal static class DuplicateTrackerDefaults
{
    internal const int DefaultShardCount = 64;
    internal static TimeSpan DefaultTrackingTime = TimeSpan.FromMinutes(1);
    internal const int DefaultMaximumCountPerShard = 1000;
}
