using GoogleIndexingService.Models;

namespace GoogleIndexingService.Services.Interfaces;

public interface IIndexingStateStore
{
    Task<IndexingState> LoadAsync(CancellationToken stoppingToken);
    Task SaveAsync(IndexingState state, CancellationToken stoppingToken);
}
