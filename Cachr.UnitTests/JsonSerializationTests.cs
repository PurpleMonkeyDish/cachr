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
        yield return CacheGossipMessage.Create(Guid.NewGuid(), Enumerable.Empty<PeerStateUpdateMessage>());
        yield return CacheGossipMessage.Create(Guid.NewGuid(), new[] { PeerStateUpdateMessage.Create(PeerState.Full, fakePeer,  new[]
            {
                fakePeer,
                fakePeer,
                fakePeer,
                fakePeer with { EndPoints = Enumerable.Range(0, 100).Select(x => x.ToString()).ToImmutableArray() }
            }) });
    }

    [Theory]
    [MemberData(nameof(GenerateSerializerTestCases))]
    public void CacheMessageContextCanDecodeItsOwnOutput(CacheGossipMessage message)
    {

        var jsonTypeInfo = CacheMessageContext.Default.CacheGossipMessage;
        Assert.NotNull(jsonTypeInfo);
        var data = JsonSerializer.SerializeToUtf8Bytes(message, jsonTypeInfo);
        var jsonString = Encoding.UTF8.GetString(data);
        _testOutputHelper.WriteLine(jsonString);

        Assert.NotEmpty(data);
        var decodedObject = JsonSerializer.Deserialize(data, jsonTypeInfo);
        var nextData = JsonSerializer.SerializeToUtf8Bytes(decodedObject, jsonTypeInfo);

        Assert.Equal((IEnumerable<byte>)data, nextData);
    }
}
