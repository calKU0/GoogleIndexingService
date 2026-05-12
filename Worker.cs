using GoogleIndexingService.Models;
using GoogleIndexingService.Services.Interfaces;
using GoogleIndexingService.Settings;
using Microsoft.Extensions.Options;

namespace GoogleIndexingService;

public sealed class Worker(
    ILogger<Worker> logger,
    IOptions<AppSettings> options,
    IUrlProvider urlProvider,
    IIndexingServiceClient indexingClient,
    IIndexingStateStore stateStore) : BackgroundService
{
    private readonly AppSettings _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Indexing service starting. Max quota: {DailyQuota} URLs.", _options.DailyQuota);

        try
        {
            var urls = await urlProvider.GetUrlsAsync(stoppingToken);
            if (urls.Count == 0)
            {
                logger.LogWarning("No URLs available for indexing.");
                return;
            }

            logger.LogInformation("Found {UrlCount} URLs to index.", urls.Count);

            var state = await stateStore.LoadAsync(stoppingToken);
            var today = DateTimeOffset.UtcNow.Date;

            // Reset daily count on new day, but preserve URL position
            if (state.LastRunDate.Date != today)
            {
                logger.LogInformation("New day detected. Resetting daily count from {PreviousCount}.", state.DailyCount);
                state.DailyCount = 0;
            }

            // If we've already hit quota for today, skip
            if (state.DailyCount >= _options.DailyQuota)
            {
                logger.LogInformation("Daily quota already reached ({Count}/{Quota}). Skipping indexing.", state.DailyCount, _options.DailyQuota);
                return;
            }

            int indexedCount = 0;
            int failureCount = 0;
            int startIndex = state.NextIndex;

            logger.LogInformation("Resuming from URL index {StartIndex} (yesterday indexed {DailyCount} URLs).", startIndex, state.DailyCount);

            for (int i = startIndex; i < urls.Count && state.DailyCount + indexedCount < _options.DailyQuota; i++)
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    logger.LogInformation("Indexing cancelled by host.");
                    break;
                }

                var url = urls[i];
                var result = await indexingClient.PublishUrlAsync(url, stoppingToken);

                if (result == IndexingResult.Success)
                {
                    indexedCount++;
                    state.NextIndex = (i + 1) % urls.Count; // Move to next URL, wrap around at end
                }
                else if (result == IndexingResult.QuotaExceeded)
                {
                    logger.LogInformation("Quota exceeded after indexing {Count} more URLs (total today: {Total}).", indexedCount, state.DailyCount + indexedCount);
                    state.NextIndex = (i + 1) % urls.Count; // Save position for next run
                    break;
                }
                else
                {
                    failureCount++;
                    state.NextIndex = i + 1; // Skip failed URLs and try next
                }
            }

            logger.LogInformation("Indexing completed: {Success} succeeded, {Failures} failed. Total today: {Total}/{Quota}.", 
                indexedCount, failureCount, state.DailyCount + indexedCount, _options.DailyQuota);

            state.DailyCount += indexedCount;
            state.LastRunDate = today;
            await stateStore.SaveAsync(state, stoppingToken);

            logger.LogInformation("State saved. Next run will start from URL index {NextIndex}.", state.NextIndex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Indexing service encountered an error.");
            throw;
        }
    }
}
