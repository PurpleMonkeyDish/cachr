using System.Diagnostics;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using Cachr.Core;
using Cachr.Core.Protocol;

namespace Cachr.Benchmarks;

// ReSharper disable once ClassCanBeSealed.Global
[MemoryDiagnoser]
public class BitEncoderBenchmarks
{
    [Benchmark]
    public void SmallNumber()
    {
        const long num = -1;
        Span<byte> bytes = stackalloc byte[BitEncoder.MaximumBytes];
        unchecked
        {
            BitEncoder.Encode7Bit(bytes, (ulong)num);
            BitEncoder.TryDecode7BitUInt64(bytes, out var value, out _);
            var longValue = (long)value;
            Debug.Assert(longValue == num, "longValue == num");
        }
    }

    [Benchmark]
    public void LargeNumber()
    {
        const long num = long.MaxValue;
        Span<byte> bytes = stackalloc byte[BitEncoder.MaximumBytes];
        unchecked
        {
            BitEncoder.Encode7Bit(bytes, (ulong)num);
            BitEncoder.TryDecode7BitUInt64(bytes, out var value, out _);
            var longValue = (long)value;
            Debug.Assert(longValue == num, "longValue == num");
        }
    }

    [Benchmark]
    public void LargeNegativeNumber()
    {
        const long num = long.MinValue;
        Span<byte> bytes = stackalloc byte[BitEncoder.MaximumBytes];
        unchecked
        {
            BitEncoder.Encode7Bit(bytes, (ulong)num);
            BitEncoder.TryDecode7BitUInt64(bytes, out var value, out _);
            var longValue = (long)value;
            Debug.Assert(longValue == num, "longValue == num");
        }
    }

    [Benchmark]
    public void Zero()
    {
        const long num = 0;
        Span<byte> bytes = stackalloc byte[BitEncoder.MaximumBytes];
        unchecked
        {
            BitEncoder.Encode7Bit(bytes, (ulong)num);
            BitEncoder.TryDecode7BitUInt64(bytes, out var value, out _);
            var longValue = (long)value;
            Debug.Assert(longValue == num, "longValue == num");
        }
    }

    [Benchmark(Baseline = true)]
    public void BaselineBenchmark()
    {
        var obj = new object();
        GC.KeepAlive(obj);
    }
}
