namespace Cachr.Core;

public sealed record InboundCacheMessageEnvelope(Guid Sender, Guid? Target, IDistributedCacheMessage Message);
