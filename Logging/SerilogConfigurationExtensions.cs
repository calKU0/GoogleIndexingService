using GoogleIndexingService.Constants;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Sinks.Email;
using System.Net;

namespace GoogleIndexingService.Logging;

public static class SerilogConfigurationExtensions
{
    private const string DefaultOutputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";
    public static LoggerConfiguration ConfigureServiceLogging(this LoggerConfiguration loggerConfiguration, IConfiguration configuration)
    {
        var logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        var logsExpirationDays = Convert.ToInt32(configuration["AppSettings:LogsExpirationDays"] ?? "14");
        Directory.CreateDirectory(logDirectory);

        loggerConfiguration
            .MinimumLevel.Information()
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.WithProperty("MachineName", Environment.MachineName)
            .Enrich.WithProperty("Application", ServiceConstants.ServiceName)
            .WriteTo.Console()
            .WriteTo.File(
                path: Path.Combine(logDirectory, "log-.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: logsExpirationDays,
                shared: true,
                outputTemplate: DefaultOutputTemplate);

        ConfigureEmailSink(loggerConfiguration, configuration);
        ConfigureSeqSink(loggerConfiguration, configuration);

        return loggerConfiguration;
    }

    private static void ConfigureEmailSink(LoggerConfiguration loggerConfiguration, IConfiguration configuration)
    {
        var emailFrom = configuration["SerilogEmail:From"];
        var emailTo = configuration["SerilogEmail:To"];
        var emailServer = configuration["SerilogEmail:MailServer"];
        var emailRecipients = (emailTo ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (string.IsNullOrWhiteSpace(emailFrom)
            || emailRecipients.Count == 0
            || string.IsNullOrWhiteSpace(emailServer))
        {
            return;
        }

        var emailSinkOptions = new EmailSinkOptions
        {
            From = emailFrom,
            To = emailRecipients,
            Host = emailServer,
            Port = Convert.ToInt32(configuration["SerilogEmail:Port"] ?? "587"),
            Subject = new MessageTemplateTextFormatter(configuration["SerilogEmail:Subject"] ?? $"{ServiceConstants.ServiceName} - Errors", null),
            Body = new MessageTemplateTextFormatter(DefaultOutputTemplate, null),
            IsBodyHtml = false,
            ConnectionSecurity = Convert.ToBoolean(configuration["SerilogEmail:EnableSsl"] ?? "true")
                ? MailKit.Security.SecureSocketOptions.StartTls
                : MailKit.Security.SecureSocketOptions.None,
            Credentials = new NetworkCredential(
                configuration["SerilogEmail:Username"] ?? string.Empty,
                configuration["SerilogEmail:Password"] ?? string.Empty)
        };

        loggerConfiguration.WriteTo.Email(
            options: emailSinkOptions,
            batchingOptions: new()
            {
                BatchSizeLimit = Convert.ToInt32(configuration["SerilogEmail:BatchPostingLimit"] ?? "50"),
                BufferingTimeLimit = TimeSpan.FromMinutes(Convert.ToDouble(configuration["SerilogEmail:BatchPostingPeriodMinutes"] ?? "60")),
                EagerlyEmitFirstEvent = false
            },
            restrictedToMinimumLevel: LogEventLevel.Warning);
    }

    private static void ConfigureSeqSink(LoggerConfiguration loggerConfiguration, IConfiguration configuration)
    {
        var seqServerUrl = configuration["SerilogSeq:ServerUrl"];

        if (string.IsNullOrWhiteSpace(seqServerUrl))
        {
            return;
        }

        loggerConfiguration.WriteTo.Seq(
            serverUrl: seqServerUrl,
            apiKey: configuration["SerilogSeq:ApiKey"],
            restrictedToMinimumLevel: LogEventLevel.Information);
    }
}

