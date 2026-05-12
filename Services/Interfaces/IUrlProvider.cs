namespace GoogleIndexingService.Services.Interfaces;

public interface IUrlProvider
{
    Task<IReadOnlyList<string>> GetUrlsAsync(CancellationToken stoppingToken);
}
