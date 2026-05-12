using System.Xml.Linq;
using GoogleIndexingService.Services.Interfaces;
using GoogleIndexingService.Settings;
using Microsoft.Extensions.Options;

namespace GoogleIndexingService.Services;

public sealed class UrlProvider(ILogger<UrlProvider> logger, IOptions<AppSettings> options) : IUrlProvider
{
    private readonly AppSettings _options = options.Value;

    public async Task<IReadOnlyList<string>> GetUrlsAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.SitemapUrl))
        {
            logger.LogWarning("No SitemapUrl configured.");
            return [];
        }

        return await LoadUrlsFromSitemapAsync(stoppingToken);
    }

    private async Task<List<string>> LoadUrlsFromSitemapAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var client = new HttpClient();
            var response = await client.GetAsync(_options.SitemapUrl, stoppingToken);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync(stoppingToken);
            var document = XDocument.Parse(content);

            XNamespace ns = document.Root?.Name.Namespace ?? string.Empty;
            var urlEntries = document.Descendants(ns + "url")
                .Select(url => new
                {
                    Location = url.Element(ns + "loc")?.Value.Trim(),
                    Priority = ParsePriority(url.Element(ns + "priority")?.Value)
                })
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Location))
                .ToList();

            if (_options.PrioritizeSitemap)
            {
                return urlEntries
                    .OrderByDescending(entry => entry.Priority)
                    .ThenBy(entry => entry.Location, StringComparer.OrdinalIgnoreCase)
                    .Select(entry => entry.Location!)
                    .ToList();
            }

            return urlEntries
                .Select(entry => entry.Location!)
                .ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read sitemap.");
            return [];
        }
    }

    private static decimal ParsePriority(string? value)
    {
        if (decimal.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var priority))
        {
            return priority;
        }

        return 0m;
    }
}
