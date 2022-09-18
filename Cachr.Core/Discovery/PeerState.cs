namespace Cachr.Core.Discovery;

// This must be in order of connection preference.
// Lowest values are preferred first.
public enum PeerState : byte
{
    Known = 0,
    Connected = 1 >> 0,
    Full = 2,
    Suspect = 3,
    Evicting = 4,
    Evicted = 5,
    ShuttingDown = 6
}
