namespace GoogleIndexingService.Settings;

public sealed class AppSettings
{
    public string GoogleCredentials { get; init; } = string.Empty;
    public string SitemapUrl { get; init; } = string.Empty;
    public string StateFile { get; init; } = "indexing-state.json";
    public int DailyQuota { get; init; } = 200;
    public int RunIntervalMinutes { get; init; } = 5;
    public bool PrioritizeSitemap { get; init; } = false;
    public int LogsExpirationDays { get; init; } = 14;
}
