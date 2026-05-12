using System.Text.Json;
using GoogleIndexingService.Models;
using GoogleIndexingService.Services.Interfaces;
using GoogleIndexingService.Settings;
using Microsoft.Extensions.Options;

namespace GoogleIndexingService.Services;

public sealed class IndexingStateStore(IOptions<AppSettings> options) : IIndexingStateStore
{
    private readonly AppSettings _options = options.Value;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IndexingState> LoadAsync(CancellationToken stoppingToken)
    {
        var statePath = Path.Combine(AppContext.BaseDirectory, _options.StateFile);
        if (!File.Exists(statePath))
        {
            return new IndexingState();
        }

        await using var stream = File.OpenRead(statePath);
        var state = await JsonSerializer.DeserializeAsync<IndexingState>(stream, _jsonOptions, stoppingToken);
        return state ?? new IndexingState();
    }

    public async Task SaveAsync(IndexingState state, CancellationToken stoppingToken)
    {
        var statePath = Path.Combine(AppContext.BaseDirectory, _options.StateFile);
        await using var stream = File.Create(statePath);
        await JsonSerializer.SerializeAsync(stream, state, _jsonOptions, stoppingToken);
    }
}
