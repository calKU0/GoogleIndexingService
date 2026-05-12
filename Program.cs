using GoogleIndexingService;
using GoogleIndexingService.Constants;
using GoogleIndexingService.Logging;
using GoogleIndexingService.Services;
using GoogleIndexingService.Services.Interfaces;
using GoogleIndexingService.Settings;
using Serilog;

var host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = ServiceConstants.ServiceName;
    })
    .UseSerilog((hostContext, _, loggerConfiguration) =>
    {
        loggerConfiguration.ConfigureServiceLogging(hostContext.Configuration);
    })
    .ConfigureServices((hostContext, services) =>
    {
        var configuration = hostContext.Configuration;

        // Configuration
        services.Configure<AppSettings>(configuration.GetSection("AppSettings"));

        // Services
        services.AddSingleton<IUrlProvider, UrlProvider>();
        services.AddSingleton<IIndexingServiceClient, IndexingServiceClient>();
        services.AddSingleton<IIndexingStateStore, IndexingStateStore>();

        // Background worker
        services.AddHostedService<Worker>();

        // Host options
        services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromSeconds(15));
    })
    .Build();

host.Run();