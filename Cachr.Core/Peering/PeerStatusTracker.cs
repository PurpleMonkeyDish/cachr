using Cachr.Core.Discovery;

namespace Cachr.Core.Peering;

public sealed class PeerStatusTracker : IPeerStatusTracker
{
    private readonly object _lockToken = new object();
    private readonly Dictionary<Guid, PeerStateUpdateMessage?> _latestPeerStateUpdates = new Dictionary<Guid, PeerStateUpdateMessage?>();
    private PeerStateUpdateMessage[]? _peerStateUpdateMessageCache = Array.Empty<PeerStateUpdateMessage>();
    private readonly HashSet<Guid> _peers = new HashSet<Guid>();
    private Guid[]? _connectedPeerCache = Array.Empty<Guid>();
    private readonly Func<Guid[]> _connectedPeerRebuildDelegate;
    private readonly Func<PeerStateUpdateMessage[]> _peerStateCacheRebuildDelegate;

    public PeerStatusTracker()
    {
        _connectedPeerRebuildDelegate = () =>
        {
            lock (_lockToken)
                return _peers.ToArray();
        };

        _peerStateCacheRebuildDelegate = () =>
        {
            lock (_lockToken)
            {
                return _latestPeerStateUpdates.Values.Cast<PeerStateUpdateMessage>().ToArray();
            }
        };
    }
    public IEnumerable<PeerStateUpdateMessage> GetAllUpdates()
    {
        return GetFromCacheOrRebuild(ref _peerStateUpdateMessageCache, _peerStateCacheRebuildDelegate);
    }

    public IEnumerable<Guid> GetConnectedPeers() => GetFromCacheOrRebuild(ref _connectedPeerCache, _connectedPeerRebuildDelegate);

    private T GetFromCacheOrRebuild<T>(ref T? field, Func<T> builder)
        where T : class
    {
        var value = field;
        if (value is not null)
            return value;

        var returnValue = builder.Invoke();
        Interlocked.CompareExchange(ref field, null, returnValue);
        return returnValue;
    }

    public void NotifyPeerStateUpdate(PeerStateUpdateMessage message)
    {
        var existing = GetAllUpdates().FirstOrDefault(i => i.Peer.Id == message.Peer.Id);
        var isMarkedConnected = GetConnectedPeers().Any(i => i == message.Peer.Id);
        if (existing is not null && ( existing.TimeStamp > message.TimeStamp || existing.Id == message.Id))
            return; // We have a newer record, or this is a duplicate.
        if (message.State > PeerState.Suspect)
        {
            if (!isMarkedConnected) return;
            RemovePeerMessage(message, isMarkedConnected, existing);
            return;
        }

        lock (_lockToken)
        {
            existing = _latestPeerStateUpdates.GetValueOrDefault(message.Peer.Id);
            // Another thread updated first. Not a big deal.
            if (existing != default && existing.TimeStamp >= message.TimeStamp)
                return;

            _latestPeerStateUpdates[message.Peer.Id] = message;
        }
        InvalidateCache(connected: false);
    }

    private void InvalidateCache(bool state = true, bool connected = true)
    {
        if(state)
            Interlocked.Exchange(ref _peerStateUpdateMessageCache, null);
        if (connected)
            Interlocked.Exchange(ref _connectedPeerCache, null);
    }

    private void RemovePeerMessage(PeerStateUpdateMessage message, bool isMarkedConnected,
        PeerStateUpdateMessage? existing)
    {
        if (existing == null && !isMarkedConnected) return;
        lock (_lockToken)
        {
            // Have to re-fetch existing and isMarkedConnected (if false) under the lock, to make 100% sure.
            existing = _latestPeerStateUpdates.GetValueOrDefault(message.Peer.Id, null);
            isMarkedConnected = isMarkedConnected || _peers.Contains(message.Peer.Id);
            // Another thread updated first. Not a big deal.
            if (existing != null && existing.TimeStamp >= message.TimeStamp)
                return;
            if (isMarkedConnected)
                _peers.Remove(message.Peer.Id);
            if (existing != null)
            {
                _latestPeerStateUpdates.Remove(existing.Id);
            }
        }

        InvalidateCache(state: existing != null, connected: isMarkedConnected);
    }

    public void NotifyNewConnection(Guid id)
    {
        if (GetConnectedPeers().Any(i => i == id)) return;
        lock (_lockToken)
        {
            _peers.Add(id);
        }
        InvalidateCache(state: false);
    }

    public void NotifyConnectionLost(Guid id)
    {
        if (GetConnectedPeers().All(i => i != id))
            return;
        lock (_lockToken)
        {
            _peers.Remove(id);
        }
        InvalidateCache(state: false);
    }
}
