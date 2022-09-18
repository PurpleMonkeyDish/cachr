using Cachr.Core.Buffers;

namespace Cachr.Core;

public record struct OutboundCacheMessageEnvelope(Guid? Target, IDistributedCacheMessage Message);
