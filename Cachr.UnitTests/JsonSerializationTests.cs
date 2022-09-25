using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.Json;
using Cachr.Core.Buffers;
using Cachr.Core.Discovery;
using Xunit;
using Xunit.Abstractions;

namespace Cachr.UnitTests;

public sealed class JsonSerializationTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public JsonSerializationTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public static IEnumerable<object[]> GenerateSerializerTestCases()
    {
        foreach (var testCase in GetGossipMessages())
            yield return new object[] {testCase};
    }

    private static IEnumerable<CacheGossipMessage> GetGossipMessages()
    {
        var fakePeer = new Peer(Guid.NewGuid(), new[] {"127.0.0.1:5001"}.ToImmutableArray(), "unknown");
        yield return CacheGossipMessage.Create(Guid.Empty, RentedArray<byte>.Empty);
        yield return CacheGossipMessage.Create(Guid.NewGuid(), RentedArray<byte>.FromDefaultPool(16));
        yield return CacheGossipMessage.Create(Guid.NewGuid(), RentedArray<byte>.FromDefaultPool(8192));
        yield return CacheGossipMessage.Create(Guid.NewGuid(), PeerStateUpdateMessage.Create(PeerState.Full, fakePeer, Array.Empty<Guid>(), Array.Empty<Guid>()));
    }

    [Theory]
    [MemberData(nameof(GenerateSerializerTestCases))]
    public void CacheMessageContextCanDecodeItsOwnOutput(CacheGossipMessage message)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(message);
        var jsonString = Encoding.UTF8.GetString(data);
        _testOutputHelper.WriteLine(jsonString);

        Assert.NotEmpty(data);
        var decodedObject = JsonSerializer.Deserialize<CacheGossipMessage>(data);
        var nextData = JsonSerializer.SerializeToUtf8Bytes(decodedObject);

        Assert.Equal((IEnumerable<byte>)data, nextData);
    }
}
