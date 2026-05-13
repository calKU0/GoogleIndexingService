using Google.Apis.Auth.OAuth2;
using Google.Apis.Indexing.v3;
using Google.Apis.Indexing.v3.Data;
using Google.Apis.Services;
using GoogleIndexingService.Models;
using GoogleIndexingService.Services.Interfaces;
using GoogleIndexingService.Settings;
using Microsoft.Extensions.Options;
using System.Net;

namespace GoogleIndexingService.Services;

public sealed class IndexingServiceClient(ILogger<IndexingServiceClient> logger, IOptions<AppSettings> options) : IIndexingServiceClient
{
    private readonly AppSettings _options = options.Value;
    private IndexingService? _service;

    public async Task<IndexingResult> PublishUrlAsync(string url, CancellationToken stoppingToken)
    {
        if (_service is null)
        {
            _service = await CreateIndexingServiceAsync(stoppingToken);
        }

        try
        {
            var notification = new UrlNotification
            {
                Url = url,
                Type = "URL_UPDATED"
            };

            var request = _service.UrlNotifications.Publish(notification);
            var response = await request.ExecuteAsync(stoppingToken);

            if (response.UrlNotificationMetadata is null)
            {
                logger.LogWarning("No metadata returned for {Url}.", url);
                return IndexingResult.Failure;
            }

            logger.LogInformation("Indexed {Url}.", url);
            return IndexingResult.Success;
        }
        catch (Google.GoogleApiException ex)
        {
            if (ex.HttpStatusCode == HttpStatusCode.TooManyRequests)
            {
                logger.LogWarning("Quota exceeded while indexing {Url}.", url);
                return IndexingResult.QuotaExceeded;
            }

            logger.LogError(ex, "Google API error while indexing {Url}.", url);
            return IndexingResult.Failure;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to index {Url}.", url);
            return IndexingResult.Failure;
        }
    }

    private async Task<IndexingService> CreateIndexingServiceAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.GoogleCredentials))
        {
            throw new InvalidOperationException(
                "AppSettings:GoogleCredentials is required.");
        }

        var serviceCredential = await CredentialFactory.FromFileAsync<ServiceAccountCredential>(Path.Combine(AppContext.BaseDirectory, _options.GoogleCredentials), stoppingToken);

        var googleCredential = GoogleCredential
            .FromServiceAccountCredential(serviceCredential)
            .CreateScoped("https://www.googleapis.com/auth/indexing");

        var initializer = new BaseClientService.Initializer
        {
            HttpClientInitializer = googleCredential,
            ApplicationName = "GoogleIndexingService"
        };

        return new IndexingService(initializer);
    }
}
