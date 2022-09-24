using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using Cachr.Core.Messages.Duplication;

namespace Cachr.Benchmarks;

[JsonExporterAttribute.Full]
[JsonExporterAttribute.FullCompressed]
[MemoryDiagnoser]
[SuppressMessage("ReSharper", "ClassCanBeSealed.Global", Justification = "Required by BenchmarkDotNet")]
public class DuplicateTrackerBenchmarks
{
    private DuplicateTracker<int> _tracker = new DuplicateTracker<int>();

    public sealed record TestRange(int Count)
    {
        public override string ToString() => $"{Count}";
    }

    public IEnumerable<TestRange> NumberRanges()
    {
        yield return new TestRange(10);

        yield return new TestRange(1000);
    }


    [Benchmark(Baseline = true)]
    [ArgumentsSource(nameof(NumberRanges))]
    public void DuplicateDetectorAddEntry(TestRange range)
    {
        for (var x = 0; x < range.Count; x++)
        {
            _tracker.IsDuplicate(x);
        }
    }

    [Benchmark]
    [ArgumentsSource(nameof(NumberRanges))]
    public void DuplicateDetectorDuplicate(TestRange range)
    {
        _tracker.IsDuplicate(0);
        for (var x = 0; x < range.Count; x++)
        {
            _tracker.IsDuplicate(0);
        }
    }

    [Benchmark(OperationsPerInvoke = 128)]
    [ArgumentsSource(nameof(NumberRanges))]
    public void DuplicateDetectorAllShardsDuplicateDetection(TestRange range)
    {
        for (var y = 0; y < range.Count; y++)
        for (var x = 0; x < 128; x++)
            _tracker.IsDuplicate(x);
    }

    [Benchmark(OperationsPerInvoke = 4)]
    [ArgumentsSource(nameof(NumberRanges))]
    public void LockContention(TestRange range)
    {
        Enumerable.Range(0, range.Count * 4).AsParallel().WithDegreeOfParallelism(4)
            .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
            .WithMergeOptions(ParallelMergeOptions.NotBuffered).Select(x => _tracker.IsDuplicate(x & 3))
            .ToArray();
    }
}
