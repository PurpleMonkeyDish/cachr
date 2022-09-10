using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cachr.Core;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Cachr.UnitTests;

public class ShardedMemoryCacheTests
{
    public static class NonNullStringGenerator
    {
        // ReSharper disable once UnusedMember.Global
        public static Arbitrary<string> Generate() =>
            Arb.Default.String().Filter(s => s is not null);
    }

    public static class NonNullByteArrayGenerator
    {
        // ReSharper disable once UnusedMember.Global
        public static Arbitrary<byte[]> Generate() =>
            Arb.Default.Array<byte>().Filter(b => b is not null);
    }

    public static class ShardPowerGenerator
    {
        // ReSharper disable once UnusedMember.Global
        public static Arbitrary<int> Generate() =>
            Arb.Default.Int32().Filter(i => i is >= 0 and <= 12);
    }

    [Property(Arbitrary = new[] {typeof(ShardPowerGenerator)})]
    public void ShardedMemoryCacheThrowsObjectDisposedWhenDisposed(int shardPower)
    {
        var cache = new ShardedMemoryCacheStorage(new CachrDistributedCacheOptions()
        {
            Shards = shardPower
        }, new NullLoggerFactory());
        Assert.NotNull(cache.MemoryCaches);
        cache.Dispose();
        Assert.Null(cache.MemoryCaches);
        Assert.Throws<ObjectDisposedException>(() => cache.Set("foo", Array.Empty<byte>()));
        Assert.Throws<ObjectDisposedException>(() => cache.TryGet("foo", out _));
        Assert.Throws<ObjectDisposedException>(() => cache.Remove("foo"));
        cache.Dispose();
        Assert.Null(cache.MemoryCaches);
    }


    [Property(
        Arbitrary = new[]
            {typeof(NonNullStringGenerator), typeof(NonNullByteArrayGenerator), typeof(ShardPowerGenerator)},
        MaxTest = 500)]
    public void CacheStorageAndRetrievalPropertyTest(string key, byte[] data, int shardPower)
    {
        var cache = new ShardedMemoryCacheStorage(new CachrDistributedCacheOptions()
        {
            Shards = shardPower
        }, new NullLoggerFactory());

        cache.Set(key, data);

        Assert.True(cache.TryGet(key, out var actualData));
        var dataFromGet = cache.Get(key);
        Assert.NotNull(dataFromGet);
        Assert.NotNull(actualData);
        Assert.Equal(data, actualData);
        Assert.Equal(data, dataFromGet);
        cache.Remove(key);
        Assert.False(cache.TryGet(key, out actualData));
        Assert.Null(cache.Get(key));
        Assert.Null(actualData);
    }

    [Property(
        Arbitrary = new[]
            {typeof(NonNullStringGenerator), typeof(NonNullByteArrayGenerator), typeof(ShardPowerGenerator)},
        MaxTest = 500)]
    public void KeyStorageAndRetrievalPropertyTest(string key, byte[] data, int shardPower)
    {
        var cache = new ShardedMemoryCacheStorage(new CachrDistributedCacheOptions()
        {
            Shards = shardPower
        }, new NullLoggerFactory());

        Assert.DoesNotContain(key, cache.Keys);
        cache.Set(key, data);
        Assert.Contains(key, cache.Keys);

        Assert.Contains(key, cache.Keys);
        cache.Remove(key);
        Assert.DoesNotContain(key, cache.Keys);
    }

    [Property(Arbitrary = new[] {typeof(ShardPowerGenerator)})]
    public void CanEnumerateKeys(int shardPower, byte keyCount)
    {
        if (keyCount == 0) return;
        var cache = new ShardedMemoryCacheStorage(new CachrDistributedCacheOptions()
        {
            Shards = shardPower
        }, new NullLoggerFactory());

        Parallel.ForEach(Enumerable.Range(0, keyCount),
            new ParallelOptions() {MaxDegreeOfParallelism = (shardPower * 100) + 1},
            i => { cache.Set($"key{i}", Array.Empty<byte>()); });

        var allKeys = cache.Keys.ToArray();
        Assert.NotNull(allKeys);
        Assert.Equal(keyCount, allKeys.Length);
    }


    [Property(Arbitrary = new[] {typeof(ShardPowerGenerator)})]
    public void CanEnumerateEntries(int shardPower, byte keyCount)
    {
        if (keyCount == 0) return;
        var cache = new ShardedMemoryCacheStorage(new CachrDistributedCacheOptions()
        {
            Shards = shardPower
        }, new NullLoggerFactory());

        Parallel.ForEach(Enumerable.Range(0, keyCount),
            new ParallelOptions() {MaxDegreeOfParallelism = (shardPower * 100) + 1},
            i => { cache.Set($"key{i}", new[] {(byte) i}); });

        var allEntries = cache.AllEntries.ToArray();
        Assert.NotNull(allEntries);
        Assert.Equal(keyCount, allEntries.Length);
        Assert.All(allEntries, kvp => Assert.NotNull(kvp.Key));
        Assert.All(allEntries, kvp => Assert.NotNull(kvp.Value));
        Assert.All(allEntries, kvp => Assert.Equal(1, kvp.Value.Length));
        Assert.All(allEntries, kvp => Assert.Equal($"key{kvp.Value[0]}", kvp.Key));
    }


#nullable disable
    [Fact]
    public void AllPublicMethodsThrowWhenKeyIsNull()
    {
        var cache = new ShardedMemoryCacheStorage(new CachrDistributedCacheOptions() {Shards = 0},
            new NullLoggerFactory());

        Assert.Throws<ArgumentNullException>("key", () => cache.Set(null, Array.Empty<byte>()));
        Assert.Throws<ArgumentNullException>("key", () => cache.TryGet(null, out _));
        Assert.Throws<ArgumentNullException>("key", () => cache.Remove(null));
    }

    [Fact]
    public void SetOperationArgumentNullExceptionHasDifferentParamNames()
    {
        var cache = new ShardedMemoryCacheStorage(new CachrDistributedCacheOptions() {Shards = 0},
            new NullLoggerFactory());

        Assert.Throws<ArgumentNullException>("obj", () => cache.Set("", null));
        Assert.Throws<ArgumentNullException>("key", () => cache.Set(null, Array.Empty<byte>()));
    }
#nullable restore

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(10, 1024)]
    [InlineData(12, 4096)]
    public void CacheShardCountCalculatesAsExpected(int power, int expectedCount)
    {
        var cache = new ShardedMemoryCacheStorage(new CachrDistributedCacheOptions()
        {
            Shards = power
        }, new NullLoggerFactory());

        Assert.NotNull(cache.MemoryCaches);
        Assert.Equal(expectedCount, cache.MemoryCaches!.Length);
    }

    [Fact]
    public void PropertiesThrowWhenDisposed()
    {
        var cache = new ShardedMemoryCacheStorage(new CachrDistributedCacheOptions()
        {
            Shards = 0
        }, new NullLoggerFactory());
        
        cache.Dispose();
        Assert.Throws<ObjectDisposedException>(() => cache.Keys.ToArray());
        Assert.Throws<ObjectDisposedException>(() => cache.AllEntries.ToArray());
    }

    [Fact]
    public void ShardedMemoryCacheThrowsWhenNegativeShardsAreConfigured()
    {
        Assert.Throws<ArgumentException>(() =>
            new ShardedMemoryCacheStorage(new CachrDistributedCacheOptions() {Shards = -1}, new NullLoggerFactory()));
    }

    [Fact]
    public void ShardedMemoryCacheThrowsWhenMemoryIsSetToLessThanOne()
    {
        Assert.Throws<ArgumentException>(() =>
            new ShardedMemoryCacheStorage(new CachrDistributedCacheOptions() {MaximumMemoryMegabytes = 0},
                new NullLoggerFactory()));
    }


    [Fact]
    public async Task CacheRemovalEventExecutesWithin100Milliseconds()
    {
        var semaphoreSlim = new SemaphoreSlim(0, 1);
        var expectedKey = string.Empty;
        var expectedEvictionReason = EvictionReason.Removed;
        string? evictedKey = null;
        EvictionReason? evictionReason = null;

        void KeyEvictedCallback(object? sender, KeyEvictedEventArgs e)
        {
            evictedKey = e.Key;
            evictionReason = e.EvictionReason;
            semaphoreSlim.Release();
        }

        var cacheStorage = new ShardedMemoryCacheStorage(new CachrDistributedCacheOptions()
            {Shards = 0, MaximumMemoryMegabytes = 256}, new NullLoggerFactory());
        cacheStorage.KeyEvicted += KeyEvictedCallback;
        cacheStorage.Set(string.Empty, Array.Empty<byte>());
        cacheStorage.Remove(string.Empty);

        Assert.True(await semaphoreSlim.WaitAsync(100));
        Assert.Equal(expectedKey, evictedKey);
        Assert.NotNull(evictionReason);
        Assert.Equal(expectedEvictionReason, evictionReason);
    }
}