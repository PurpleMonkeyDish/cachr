using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cachr.Core;
using Cachr.Core.Messages;
using Cachr.Core.Messages.Encoder;
using FsCheck;
using FsCheck.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Cachr.UnitTests;

public class EncoderTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public EncoderTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public static IEnumerable<object[]> GetDistributedCacheTestMessages()
    {
        return GetDistributedCacheMessages().Select(i => new object[] {i});
    }

    private static IEnumerable<IDistributedCacheMessage> GetDistributedCacheMessages()
    {
        yield return GetKeysDistributedCacheMessage.Instance;
        yield return new GetKeysResponseDistributedCacheMessage("Hello World");
        yield return new GetKeysResponseDistributedCacheMessage("");
        yield return new GetKeyDataDistributedCacheMessage("");
        yield return new GetKeyDataDistributedCacheMessage("Hello World");
        yield return new GetKeyDataResponseDistributedCacheMessage("", Array.Empty<byte>());
        yield return new GetKeyDataResponseDistributedCacheMessage("HelloWorld", Array.Empty<byte>());
        yield return new KeySetDistributedCacheMessage("", Array.Empty<byte>(), 0, 0);
        yield return new KeySetDistributedCacheMessage("", Array.Empty<byte>(), int.MaxValue / 2, 0);
        yield return new KeySetDistributedCacheMessage("", Array.Empty<byte>(), int.MaxValue, 0);
        yield return new KeySetDistributedCacheMessage("", Array.Empty<byte>(), 0, long.MaxValue / 2);
        yield return new KeySetDistributedCacheMessage("", Array.Empty<byte>(), 0, long.MaxValue);
        yield return new KeySetDistributedCacheMessage("", Array.Empty<byte>(), int.MaxValue / 2, long.MaxValue / 2);
        yield return new KeySetDistributedCacheMessage("", Array.Empty<byte>(), int.MaxValue, long.MaxValue);
        yield return new KeyDeletedDistributedCacheMessage("");
    }

    [Property(Arbitrary = new[] {typeof(NonNullStringGenerator)})]
    public void CanEncodeAndDecodeVariousMessageSizes(string key, byte[] data)
    {
        var message = new GetKeyDataResponseDistributedCacheMessage(key, data);
        using var encodedMessage = DistributedCacheMessageEncoder.Encode(message);
        var decodedMessage = DistributedCacheMessageEncoder.Decode(encodedMessage);
        Assert.NotNull(decodedMessage);
        var typedDecodedMessage = Assert.IsAssignableFrom<GetKeyDataResponseDistributedCacheMessage>(decodedMessage);
        Assert.Equal(message.Key, typedDecodedMessage.Key);
        Assert.Equal(message.Data, (IEnumerable<byte>)typedDecodedMessage.Data);
        _testOutputHelper.WriteLine(
            $"Encoded payload was {encodedMessage.ArraySegment.Count} bytes, for key length {message.Key.Length} and data length: {message.Data.Length}");
    }

    [Theory]
    [MemberData(nameof(GetDistributedCacheTestMessages))]
    public void CanDecodeEncodedMessage(IDistributedCacheMessage message)
    {
        using var encodedMessage = DistributedCacheMessageEncoder.Encode(message);
        var decodedMessage = DistributedCacheMessageEncoder.Decode(encodedMessage);
        Assert.NotNull(decodedMessage);

        Assert.Equal(message, decodedMessage);
    }

    [Fact]
    public void DecoderThrowsNotImplementedExceptionWhenUnknownMessageType()
    {
        using var encodedMessage = DistributedCacheMessageEncoder.Encode(new TestMessage());
        Assert.Throws<NotImplementedException>(() => DistributedCacheMessageEncoder.Decode(encodedMessage));
    }

    public static class NonNullStringGenerator
    {
        // ReSharper disable once UnusedMember.Global
        public static Arbitrary<string> Generate()
        {
            return Arb.Default.String().Filter(s => s is not null);
        }
    }

    internal sealed class TestMessage : IDistributedCacheMessage
    {
        public DistributedCacheMessageType Type { get; } = (DistributedCacheMessageType)1000;
        public Guid Id { get; } = Guid.NewGuid();
        public int MaximumWireSize { get; } = 0;

        public void Encode(BinaryWriter writer)
        {
        }
    }
}
