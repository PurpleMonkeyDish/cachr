using System;
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

    [Property(Arbitrary = new[] { typeof(ShardPowerGenerator)})]
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

    [Property(Arbitrary = new[] { typeof(NonNullStringGenerator), typeof(NonNullByteArrayGenerator), typeof(ShardPowerGenerator)})]
    public void KeyStorageAndRetrievalPropertyTest(string key, byte[] data, int shardPower)
    {
        var cache = new ShardedMemoryCacheStorage(new CachrDistributedCacheOptions()
        {
            Shards = shardPower
        }, new NullLoggerFactory());
        
        cache.Set(key, data);
        
        Assert.True(cache.TryGet(key, out var actualData));
        Assert.NotNull(actualData);
        Assert.Equal(data, actualData);
        cache.Remove(key);
        Assert.False(cache.TryGet(key, out actualData));
        Assert.Null(actualData);
    }

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
            {Shards = 0, MaximumMemoryMegabytes = 256 }, new NullLoggerFactory());
        cacheStorage.KeyEvicted += KeyEvictedCallback;
        cacheStorage.Set(string.Empty, Array.Empty<byte>());
        cacheStorage.Remove(string.Empty);

        Assert.True(await semaphoreSlim.WaitAsync(100));
        Assert.Equal(expectedKey, evictedKey);
        Assert.NotNull(evictionReason);
        Assert.Equal(expectedEvictionReason, evictionReason);
    }
}