namespace Cachr.Core.Discovery;

public enum PeerState : byte
{
    Initializing,
    Full,
    Suspect,
    Evicted,
    ShuttingDown
}
