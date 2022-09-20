using System.Text.Json.Serialization;
using Cachr.Core.Buffers;

namespace Cachr.Core.Discovery;

public sealed record CacheGossipMessage(
    CacheGossipType Type,
    Guid Id,
    Guid Sender,
    Guid? Target,
    PeerStateUpdateMessage? PeerStateUpdate,
    [property: JsonConverter(typeof(RentedArrayJsonConverterFactory))] RentedArray<byte>? Message
)
{
    public static CacheGossipMessage Create(Guid sender, PeerStateUpdateMessage peerStateUpdate)
    {
        return new(
            CacheGossipType.PeerStateUpdate,
            Guid.NewGuid(),
            sender,
            null,
            peerStateUpdate,
            null
        );
    }

    public static CacheGossipMessage Create(Guid sender, Guid target, PeerStateUpdateMessage peerStateUpdate)
    {
        return new CacheGossipMessage(
            CacheGossipType.PeerStateUpdate,
            Guid.NewGuid(),
            sender,
            target,
            peerStateUpdate,
            null
        );
    }

    public static CacheGossipMessage Create(Guid sender, RentedArray<byte> cacheCommand)
    {
        return new(
            CacheGossipType.CacheCommand,
            Guid.NewGuid(),
            sender,
            null,
            null,
            cacheCommand
        );
    }

    public static CacheGossipMessage Create(Guid sender, Guid target, RentedArray<byte> cacheCommand)
    {
        return new(
            CacheGossipType.CacheCommand,
            Guid.NewGuid(),
            sender,
            target,
            null,
            cacheCommand
        );
    }
}
