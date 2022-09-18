using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Cachr.Core.Buffers;

namespace Cachr.Core.Discovery;

public sealed record CacheGossipMessage(
    CacheGossipType Type,
    Guid Sender,
    Guid? Target,
    ImmutableArray<PeerStateUpdateMessage> PeerStateUpdates,
   [property: JsonConverter(typeof(RentedArrayJsonConverterFactory))] RentedArray<byte>? CacheCommand
)
{
    public static CacheGossipMessage Create(Guid sender, IEnumerable<PeerStateUpdateMessage> peerStateUpdates)
    {
        return new(
            CacheGossipType.PeerStateUpdate,
            sender,
            null,
            peerStateUpdates is ImmutableArray<PeerStateUpdateMessage> array
                ? array
                : peerStateUpdates.ToImmutableArray(),
            null
        );
    }

    public static CacheGossipMessage Create(Guid sender, Guid target, IEnumerable<PeerStateUpdateMessage> peerStateUpdates)
    {
        return new CacheGossipMessage(
            CacheGossipType.PeerStateUpdate,
            sender,
            target,
            peerStateUpdates is ImmutableArray<PeerStateUpdateMessage> array
                ? array
                : peerStateUpdates.ToImmutableArray(),
            null
        );
    }

    public static CacheGossipMessage Create(Guid sender, RentedArray<byte> cacheCommand)
    {
        return new(
            CacheGossipType.CacheCommand,
            sender,
            null,
            ImmutableArray<PeerStateUpdateMessage>.Empty,
            cacheCommand
        );
    }

    public static CacheGossipMessage Create(Guid sender, Guid target, RentedArray<byte> cacheCommand)
    {
        return new(
            CacheGossipType.CacheCommand,
            sender,
            target,
            ImmutableArray<PeerStateUpdateMessage>.Empty,
            cacheCommand
        );
    }
}
