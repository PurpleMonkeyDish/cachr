namespace Cachr.Core.Messaging;

[Flags]
public enum SubscriptionMode
{
    Broadcast = 1,
    Targeted = 2,
    All = Broadcast | Targeted
}