using System;
using Cachr.Core;
using Cachr.Core.Buffers;
using Cachr.Core.Messages;
using Cachr.Core.Messages.Bus;
using Cachr.Core.Messages.Encoder;
using Cachr.Core.Storage;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Cachr.UnitTests;

public class CacheMessageProcessingTests
{    
    public static class NonNullStringGenerator
    {
        // ReSharper disable once UnusedMember.Global
        public static Arbitrary<string> Generate() =>
            Arb.Default.String().Filter(s => s is not null);
    }
    private ICachrDistributedCache CreateDistributedCacheInstance(ICacheBus cacheBus)
    {
        var options = new CachrDistributedCacheOptions() {Shards = 0};
        return new CachrDistributedCache(
            new ShardedMemoryCacheStorage(options, new NullLoggerFactory()), 
            cacheBus,
            options);
    }

    [Property(Arbitrary = new[] { typeof(NonNullStringGenerator) })]
    public void EntryIsAddedWhenKeyAddBusEventIsReceived(string key)
    {
        var busMock = new Mock<ICacheBus>();
        var cache = CreateDistributedCacheInstance(busMock.Object);
        var cacheSetMessage = new KeySetDistributedCacheMessage(key, Array.Empty<byte>(), 0, 0);
        
        Assert.Null(cache.Get(key));
        SimulateMessageReceived(busMock, cacheSetMessage);
        Assert.Equal(Array.Empty<byte>(), cache.Get(key));
    }

    [Fact]
    public void PreloadRequestsFromSinglePeer()
    {
        var busMock = new Mock<ICacheBus>();
        var cache = CreateDistributedCacheInstance(busMock.Object);
        cache.BeginPreload();
        busMock.Verify(i => i.SendToOneRandom(It.IsAny<RentedArray<byte>>()), Times.Once);
    }

    [Property(Arbitrary = new[] {typeof(EncoderTests.NonNullStringGenerator)})]
    public void CacheSendsBroadcastsOnSetAndDelete(string key)
    {
        var busMock = new Mock<ICacheBus>();
        var cache = CreateDistributedCacheInstance(busMock.Object);
        RentedArray<byte>? lastSentMessage = null;
        busMock.Setup(i => i.Broadcast(It.IsAny<RentedArray<byte>>()))
            .Callback( (RentedArray<byte> ra) =>
            {
                lastSentMessage = ra;
            });
        
        
        busMock.Verify(i => i.Broadcast(It.IsAny<RentedArray<byte>>()), Times.Never);
        Assert.Null(lastSentMessage);
        cache.Set(key, Array.Empty<byte>(), null);
        busMock.Verify(i => i.Broadcast(It.IsAny<RentedArray<byte>>()), Times.Once);
        Assert.NotNull(lastSentMessage);
        var decodedMessage = DistributedCacheMessageEncoder.Decode(lastSentMessage!);
        var typedSetMessage = Assert.IsAssignableFrom<KeySetDistributedCacheMessage>(decodedMessage);
        
        Assert.Equal(key, typedSetMessage.Key);
        Assert.Equal(Array.Empty<byte>(), typedSetMessage.Data);
        Assert.Equal(0, typedSetMessage.SlidingTimeToLiveMilliseconds);
        Assert.Equal(0, typedSetMessage.ExpirationTimeStampUnixMilliseconds);
        
        cache.Remove(key);
        busMock.Verify(i => i.Broadcast(It.IsAny<RentedArray<byte>>()), Times.Exactly(2));
        Assert.NotNull(lastSentMessage);
        decodedMessage = DistributedCacheMessageEncoder.Decode(lastSentMessage!);
        var typedDeleteMessage = Assert.IsAssignableFrom<KeyDeletedDistributedCacheMessage>(decodedMessage);
        Assert.Equal(key, typedDeleteMessage.Key);
        
    }
    
    
    [Property(Arbitrary = new[] { typeof(NonNullStringGenerator) })]
    public void EntryIsDeletedWhenKeyDeleteBusEventIsReceived(string key)
    {
        var busMock = new Mock<ICacheBus>();
        var cache = CreateDistributedCacheInstance(busMock.Object);
        busMock.Verify(i => i.Broadcast(It.IsAny<RentedArray<byte>>()), Times.Never);
        cache.Set(key, Array.Empty<byte>(), null);
        busMock.Verify(i => i.Broadcast(It.IsAny<RentedArray<byte>>()), Times.Once);
        var cacheSetMessage = new KeyDeletedDistributedCacheMessage(key);
        
        Assert.NotNull(cache.Get(key));
        Assert.Equal(Array.Empty<byte>(), cache.Get(key));
        SimulateMessageReceived(busMock, cacheSetMessage);
        busMock.Verify(i => i.Broadcast(It.IsAny<RentedArray<byte>>()), Times.Once);
        Assert.Null(cache.Get(key));
    }

    void SimulateMessageReceived(Mock<ICacheBus> bus, IDistributedCacheMessage message)
    {
        var encodedMessage = DistributedCacheMessageEncoder.Encode(message);
        bus.Raise(i => i.DataReceived += null, new CacheBusDataReceivedEventArgs(string.Empty, encodedMessage, r => { }));
    }
}