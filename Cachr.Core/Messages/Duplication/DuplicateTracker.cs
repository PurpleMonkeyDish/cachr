using System.Diagnostics.CodeAnalysis;

namespace Cachr.Core.Messages.Duplication;

public sealed class DuplicateTracker<T> : IDuplicateTracker<T>
{
    private readonly int _shardCount;
    private readonly TimeSpan _trackerMaxTime;
    private readonly int _maximumCountPerShard;
    public DuplicateTracker(int? shardCount = null, int? maximumCountPerShard = null, TimeSpan? maxLifetime = null)
    {
        if (shardCount <= 0) throw new ArgumentOutOfRangeException();
        if (maximumCountPerShard <= 0) throw new ArgumentOutOfRangeException();
        _trackerMaxTime = maxLifetime ?? DuplicateTrackerDefaults.DefaultTrackingTime;
        _shardCount = shardCount ?? DuplicateTrackerDefaults.DefaultShardCount;
        _maximumCountPerShard = maximumCountPerShard ?? DuplicateTrackerDefaults.DefaultMaximumCountPerShard;

        _duplicateItemSets = new HashSet<T>[_shardCount];
        _ageTrackerShards = new List<DuplicateTrackingRecord>[_shardCount];
        for (var x = 0; x < _shardCount; x++)
        {
            _duplicateItemSets[x] = new HashSet<T>();
            _ageTrackerShards[x] = new List<DuplicateTrackingRecord>();
        }
    }

    private (HashSet<T>, List<DuplicateTrackingRecord>) GetShard(int index) =>
        (_duplicateItemSets[index], _ageTrackerShards[index]);

    private (HashSet<T>, List<DuplicateTrackingRecord>) GetShardFromItem([NotNull] T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var index = Math.Abs(item.GetHashCode() % _shardCount);
        return GetShard(index);
    }

    private List<DuplicateTrackingRecord>[] _ageTrackerShards;
    private HashSet<T>[] _duplicateItemSets;

    private record DuplicateTrackingRecord(T Item, DateTimeOffset Added);

    public bool IsDuplicate(T item)
    {
        var (set, ageTracker) = GetShardFromItem(item);
        var addedToSet = false;
        lock (set)
        {
            addedToSet = set.Add(item);
            if (addedToSet)
            {
                ageTracker.Add(new DuplicateTrackingRecord(item, DateTimeOffset.Now));
            }
        }

        var diceRoll = Random.Shared.NextDouble();
        if (diceRoll < 0.05)
            PerformCleanup();
        else if (addedToSet && diceRoll < 0.25)
            PerformCleanup(set, ageTracker);
        else if (diceRoll < 0.10)
            PerformCleanup(set, ageTracker);
        return !addedToSet;
    }

    public void Cleanup() => PerformCleanup();

    private void PerformCleanup()
    {
        for (var x = 0; x < _shardCount; x++)
        {
            var (shardSet, shardTracker) = GetShard(x);
            PerformCleanup(shardSet, shardTracker);
        }
    }

    private void PerformCleanup(HashSet<T> set, List<DuplicateTrackingRecord> ageTracker)
    {
        while (true)
        {
            lock (set)
            {
                if (set.Count == 0) break;
                if (set.Count <= 1000 &&
                    ageTracker[0].Added.Add(_trackerMaxTime) >= DateTimeOffset.Now)
                    break;
                var item = ageTracker[0];
                set.Remove(item.Item);
                ageTracker.RemoveAt(0);
            }
        }
    }
}
