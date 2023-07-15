namespace Cachr.Core.Cache;

public class ReaperConfiguration
{
    public TimeSpan ReapInterval { get; set; } = TimeSpan.FromHours(1);
    public int ReapBatchSize { get; set; }
    public int ReapPasses { get; set; } = 10;
}