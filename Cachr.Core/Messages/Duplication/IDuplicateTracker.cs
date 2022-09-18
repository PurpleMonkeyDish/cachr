using System.Diagnostics.CodeAnalysis;

namespace Cachr.Core.Messages.Duplication;

public interface IDuplicateTracker<T>
{
    bool IsDuplicate(T item);
}

public sealed class DuplicateTracker<T> : IDuplicateTracker<T>
{
    private const int Shards = 64;

    public DuplicateTracker()
    {
        _duplicateItemSets = new HashSet<T>[Shards];
        _ageTrackerShards = new List<DuplicateTrackingRecord>[Shards];
        for (var x = 0; x < Shards; x++)
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
        var index = Math.Abs(item.GetHashCode() % Shards);
        return GetShard(index);
    }

    private List<DuplicateTrackingRecord>[] _ageTrackerShards;
    private HashSet<T>[] _duplicateItemSets;

    private record DuplicateTrackingRecord(T Item, DateTimeOffset Added);

    public bool IsDuplicate(T item)
    {
        var (set, ageTracker) = GetShardFromItem(item);
        bool addedToSet = false;
        lock (set)
        {
            if (addedToSet = set.Add(item))
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
        for (var x = 0; x < Shards; x++)
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
                    ageTracker[0].Added.AddMinutes(10) >= DateTimeOffset.Now)
                    break;
                var item = ageTracker[0];
                set.Remove(item.Item);
                ageTracker.RemoveAt(0);
            }
        }
    }
}
