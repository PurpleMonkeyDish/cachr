using Cachr.Core;
using Cachr.Core.Protocol;

namespace Cachr.UnitTests;

public sealed class ProtocolParserTests
{
    [Fact]
    public async Task CanDecodeEncodedData()
    {
        var parser = new CacheProtocolCommandParser();
        var commandPacket = new CacheCommand()
        {
            Command = Command.Clear,
            CommandId = 1234,
            Arguments = new[]
            {
                new ProtocolValue(CacheType.Bytes, null), new ProtocolValue(CacheType.Bytes, new byte[32])
            }
        };
        var encodedPayload = await parser.ToByteArrayAsync(commandPacket);
        Assert.True(parser.TryParse(encodedPayload, out var decodedCommandPacket, out var consumed));
        Assert.Equal(encodedPayload.Length, consumed);
        Assert.NotNull(decodedCommandPacket);
        Assert.Equal(commandPacket.CommandId, decodedCommandPacket.CommandId);
        Assert.Equal(commandPacket.Command, decodedCommandPacket.Command);
        Assert.Equal(commandPacket.Arguments.Length, decodedCommandPacket.Arguments.Length);
        for (var x = 0; x < commandPacket.Arguments.Length; x++)
        {
            Assert.NotNull(decodedCommandPacket.Arguments[x]);
            Assert.Equal(commandPacket.Arguments[x].Type, decodedCommandPacket.Arguments[x].Type);
            if (commandPacket.Arguments[x].Value is null)
            {
                Assert.Null(decodedCommandPacket.Arguments[x].Value);
            }
            else
            {
                Assert.NotNull(commandPacket.Arguments[x].Value);
                Assert.NotNull(decodedCommandPacket.Arguments[x].Value);
                Assert.Equal(commandPacket.Arguments[x].Value!.AsEnumerable(),
                    decodedCommandPacket.Arguments[x].Value!.AsEnumerable());
            }
        }
    }
}
