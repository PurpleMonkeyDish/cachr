using System.Text.Json.Serialization;

namespace Cachr.Core.Discovery;

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Metadata | JsonSourceGenerationMode.Serialization,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
#if DEBUG
    WriteIndented = true
#else
    WriteIndented = false
#endif
)]
[JsonSerializable(typeof(Peer))]
[JsonSerializable(typeof(PeerStateUpdateMessage))]
[JsonSerializable(typeof(CacheGossipMessage))]
internal sealed partial class CacheMessageContext : JsonSerializerContext
{
}
