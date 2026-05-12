namespace GoogleIndexingService.Models;

public sealed class IndexingState
{
    public DateTimeOffset LastRunDate { get; set; } = DateTimeOffset.UtcNow.Date;
    public int DailyCount { get; set; }
    public int NextIndex { get; set; }
}
