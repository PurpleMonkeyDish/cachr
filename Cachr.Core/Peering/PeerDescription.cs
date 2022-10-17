using System.Collections.Immutable;

namespace Cachr.Core.Peering;

public sealed record PeerDescription(Guid Id, ImmutableArray<string> Uris)
{
    public PeerDescription(Guid id, IEnumerable<string> uris) : this(id, uris.ToImmutableArray()) { }
    public PeerDescription(Guid id, IEnumerable<Uri> uris) : this(id, uris.Select(i => i.ToString())) { }
}