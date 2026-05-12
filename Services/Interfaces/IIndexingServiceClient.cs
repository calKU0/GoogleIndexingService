using GoogleIndexingService.Models;

namespace GoogleIndexingService.Services.Interfaces;

public interface IIndexingServiceClient
{
    Task<IndexingResult> PublishUrlAsync(string url, CancellationToken stoppingToken);
}
