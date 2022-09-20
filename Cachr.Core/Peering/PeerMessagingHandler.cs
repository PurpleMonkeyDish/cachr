using Cachr.Core.Discovery;
using Cachr.Core.Messages.Duplication;
using Cachr.Core.Messages.Encoder;
using Cachr.Core.Messaging;

namespace Cachr.Core.Peering;

public sealed class PeerMessagingHandler : IPeerMessagingHandler, IDisposable
{
    private readonly IPeerStatusTracker _peerStatusTracker;
    private readonly ISubscriptionToken _outboundMessageSubscription;
    private readonly IMessageBus<InboundCacheMessageEnvelope> _inboundMessageBus;
    private readonly IMessageBus<CacheGossipMessage> _cacheGossipMessageBus;
    private readonly IDuplicateTracker<Guid> _duplicateTracker = new DuplicateTracker<Guid>(16, 5000, TimeSpan.FromMinutes(1));

    public PeerMessagingHandler(
        IMessageBus<InboundCacheMessageEnvelope> inboundMessageBus,
        IMessageBus<OutboundCacheMessageEnvelope> outboundMessageBus,
        IPeerStatusTracker peerStatusTracker,
        IMessageBus<CacheGossipMessage> cacheGossipMessageBus

    )
    {
        _peerStatusTracker = peerStatusTracker;
        _cacheGossipMessageBus = cacheGossipMessageBus;
        _outboundMessageSubscription = outboundMessageBus.Subscribe(OutboundMessageHandler);
        _inboundMessageBus = inboundMessageBus;
    }

    private async Task OutboundMessageHandler(OutboundCacheMessageEnvelope outboundMessage)
    {
        CacheGossipMessage cacheGossipMessage;
        var encodedMessage = DistributedCacheMessageEncoder.Encode(outboundMessage.Message);
        if (outboundMessage.Target != null)
            cacheGossipMessage = CacheGossipMessage.Create(NodeIdentity.Id, outboundMessage.Target.Value, encodedMessage);
        else
            cacheGossipMessage = CacheGossipMessage.Create(NodeIdentity.Id, encodedMessage);
        _duplicateTracker.IsDuplicate(cacheGossipMessage.Id); // We don't want to process this message.
        await _cacheGossipMessageBus.BroadcastAsync(cacheGossipMessage);
    }

    public void Dispose()
    {
        _outboundMessageSubscription.Dispose();
    }

    public async ValueTask NotifyGossipReceivedAsync(CacheGossipMessage gossipMessage)
    {
        if (_duplicateTracker.IsDuplicate(gossipMessage.Id))
            return;
        ArgumentNullException.ThrowIfNull(gossipMessage);
        // Not intended for us, ignore it.
        if (gossipMessage.Target != null && gossipMessage.Target != NodeIdentity.Id) return;
        switch (gossipMessage.Type)
        {
            case CacheGossipType.PeerStateUpdate:
                ArgumentNullException.ThrowIfNull(gossipMessage.PeerStateUpdate);

                if (gossipMessage.PeerStateUpdate.Peer.Id != gossipMessage.Sender)
                {
                    var existingPeer = _peerStatusTracker
                        .GetAllUpdates()
                        .FirstOrDefault(i => i.Peer.Id == gossipMessage.PeerStateUpdate.Peer.Id);

                    if (existingPeer is not null)
                    {
                        // Don't accept anything other than state updates and timestamps for known peers,
                        // unless the message origin is the peer it's self.
                        gossipMessage = gossipMessage with
                        {
                            PeerStateUpdate = existingPeer with
                            {
                                Id = gossipMessage.PeerStateUpdate.Id,
                                State = gossipMessage.PeerStateUpdate.State,
                                TimeStamp = gossipMessage.PeerStateUpdate.TimeStamp
                            }
                        };
                    }
                }

                _peerStatusTracker.NotifyPeerStateUpdate(gossipMessage.PeerStateUpdate);
                break;
            case CacheGossipType.CacheCommand:
                ArgumentNullException.ThrowIfNull(gossipMessage.Message);
                var decodedMessage = DistributedCacheMessageEncoder.Decode(gossipMessage.Message);
                await _inboundMessageBus.BroadcastAsync(new InboundCacheMessageEnvelope(gossipMessage.Sender, gossipMessage.Target, decodedMessage)).ConfigureAwait(false);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}