using Cachr.Core.Buffers;

namespace Cachr.Core;

public sealed record OutboundCacheMessageEnvelope(Guid? Target, IDistributedCacheMessage Message);
