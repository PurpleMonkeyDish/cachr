namespace Cachr.Core;

public class NotificationPreventer : IDisposable
{
    private static AsyncLocal<bool>? _areNotificationsSuppressed = new AsyncLocal<bool>();
    private NotificationPreventer()
    {
        _areNotificationsSuppressed!.Value = true;
    }

    public static bool AreNotificationsSuppressed => _areNotificationsSuppressed!.Value;
    public static IDisposable BeginScope() => new NotificationPreventer();
    
    

    public void Dispose()
    {
        _areNotificationsSuppressed!.Value = false;
    }
}