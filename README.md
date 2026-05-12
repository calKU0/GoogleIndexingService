# Google Indexing Service

A .NET 10 Worker Service that automatically submits URLs to Google's Indexing API from your sitemap, helping to keep Google's index fresh and up-to-date with your website content.

## Features

- 🔄 **Automatic URL Indexing** - Submits URLs from your sitemap to Google's Indexing API
- 📊 **Daily Quota Management** - Respects daily API limits (default: 200 URLs/day)
- 💾 **State Persistence** - Tracks progress and resumes from where it left off on restart
- 🔁 **Circular Indexing** - Automatically cycles through all URLs, starting from beginning when complete
- 📅 **Daily Reset** - Quota resets automatically each day
- 📝 **Comprehensive Logging** - Serilog integration with file and console output
- 📧 **Email Notifications** - Optional error notifications via email
- 🪟 **Windows Service** - Runs as a Windows Service for automated execution
- ⚡ **One-Shot Execution** - Indexes up to quota limit per run, designed for scheduled execution

## Requirements

- .NET 10 Runtime
- Google Service Account credentials (JSON file or inline)
- Valid sitemap URL
- Windows Server (for Windows Service deployment)

## Installation

### 1. Prerequisites Setup

**Create Google Service Account:**
1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select existing
3. Enable the "Indexing API"
4. Create a Service Account and download the JSON key file
5. Share your property with the service account in [Google Search Console](https://search.google.com/search-console)

### 2. Configure the Service

Edit `appsettings.json`:

```json
{
  "AppSettings": {
    "GoogleCredentials": "path-to-your-credentials.json",
    "SitemapUrl": "https://yourdomain.com/sitemap.xml",
    "StateFile": "indexing-state.json",
    "DailyQuota": 200,
    "RunIntervalMinutes": 5,
    "LogsExpirationDays": 14,
    "PrioritizeSitemap": true
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "SerilogEmail": {
    "From": "",
    "To": "",
    "MailServer": "",
    "Port": 587,
    "EnableSsl": true,
    "Username": "",
    "Password": "",
    "Subject": "Google Indexing Service - Errors",
    "BatchPostingLimit": 100,
    "BatchPostingPeriodMinutes": 60
  },
  "SerilogSeq": {
    "ServerUrl": "",
    "ApiKey": ""
  }
}
```

**Configuration Options:**

| Setting | Description | Example |
|---------|-------------|---------|
| `GoogleCredentials` | Path to Google Service Account JSON file | `google-key.json` |
| `SitemapUrl` | URL to your sitemap.xml | `https://yoursite.com/sitemap.xml` |
| `StateFile` | Path to state tracking file (relative to AppContext.BaseDirectory) | `indexing-state.json` |
| `DailyQuota` | Maximum URLs to index per day (Google's limit: 200/day) | `200` |
| `RunIntervalMinutes` | Unused (service runs once per execution) | `5` |
| `LogsExpirationDays` | Days to retain log files | `14` |
| `PrioritizeSitemap` | Sort URLs by sitemap priority if available | `true` |

**Email Notifications (Optional):**
```json
"SerilogEmail": {
  "From": "noreply@yourdomain.com",
  "To": "admin@yourdomain.com",
  "MailServer": "smtp.yourdomain.com",
  "Port": 587,
  "EnableSsl": true,
  "Username": "your-email@yourdomain.com",
  "Password": "your-password",
  "Subject": "Google Indexing Service - Errors",
  "BatchPostingLimit": 100,
  "BatchPostingPeriodMinutes": 60
}
```

**Seq Integration (Optional):**
```json
"SerilogSeq": {
  "ServerUrl": "https://seq.yourdomain.com",
  "ApiKey": "your-seq-api-key"
}
```

### 3. Place Credentials File

Place your Google Service Account JSON file in the application directory:

```
C:\Services\GoogleIndexingService\
├── GoogleIndexingService.exe
├── appsettings.json
├── google-key.json          ← Your credentials here
└── indexing-state.json        ← Auto-created on first run
```

### 4. Install as Windows Service

```powershell
# Build the project
dotnet publish -c Release -o "C:\Services\GoogleIndexingService"

# Copy your credentials file
Copy-Item "google-key.json" -Destination "C:\Services\GoogleIndexingService\"

# Install as Windows Service
sc.exe create GoogleIndexingService binPath= "C:\Services\GoogleIndexingService\GoogleIndexingService.exe"

# Set to run as NETWORK SERVICE (or appropriate account)
sc.exe config GoogleIndexingService obj= "NT AUTHORITY\NETWORK SERVICE"

# Grant file access permissions (optional)
icacls "C:\Services\GoogleIndexingService" /grant "NT AUTHORITY\NETWORK SERVICE:(OI)(CI)F" /T

# Start the service
net start GoogleIndexingService
```

## Usage

### Running Standalone

```bash
dotnet GoogleIndexingService.dll
```

### Running as Windows Service

```powershell
# Start
net start GoogleIndexingService

# Stop
net stop GoogleIndexingService

# Check status
sc.exe query GoogleIndexingService

# View recent logs
Get-Content "Logs\indexing-*.log" -Tail 50
```

### Scheduled Execution

**Daily at 2 AM using Task Scheduler:**

1. Open Task Scheduler
2. Create Basic Task
3. Set trigger: Daily at 2:00 AM
4. Set action: 
   - Program: `C:\Services\GoogleIndexingService\GoogleIndexingService.exe`
   - Start in: `C:\Services\GoogleIndexingService`

## How It Works

### Single Run Flow

1. **Load Configuration** - Reads appsettings.json
2. **Load Credentials** - Reads Google Service Account JSON file
3. **Fetch Sitemap** - Downloads and parses your sitemap.xml
4. **Check Quota** - Verifies daily quota hasn't been reached
5. **Resume Position** - Starts from NextIndex (where previous run ended)
6. **Index URLs** - Submits each URL to Google's Indexing API
7. **Track Progress** - Updates position and daily count
8. **Save State** - Persists progress to indexing-state.json
9. **Wrap Around** - After indexing all URLs, starts from beginning on next day

### State Management

State is tracked in `indexing-state.json`:

```json
{
  "LastRunDate": "2024-01-15T00:00:00Z",
  "DailyCount": 45,
  "NextIndex": 50
}
```

- **LastRunDate** - Last execution date (resets daily count if new day)
- **DailyCount** - URLs indexed today
- **NextIndex** - Position to resume from on next run

## Logging

Logs are written to:
- **Console** - Real-time output for debugging
- **Files** - `Logs/indexing-YYYY-MM-DD.log` (rolling daily, keeps 14 days)
- **Email** - Optional error notifications (if configured)
- **Seq** - Optional centralized logging (if configured)

### Log Examples

**Successful indexing:**
```
[10:28:29 INF] Indexing service starting. Max quota: 200 URLs.
[10:28:29 INF] Found 1000 URLs to index.
[10:28:29 INF] Resuming from URL index 200 (yesterday indexed 200 URLs).
[10:28:29 INF] Indexed https://yourdomain.com/page-1.
[10:28:30 INF] Indexed https://yourdomain.com/page-2.
[10:28:31 INF] Indexing completed: 200 succeeded, 0 failed. Total today: 200/200.
[10:28:31 INF] State saved. Next run will start from URL index 400.
```

**Quota exceeded:**
```
[10:28:35 WRN] Quota exceeded while indexing https://yourdomain.com/page-5.
[10:28:35 INF] Quota exceeded after indexing 4 more URLs (total today: 204).
```

## Troubleshooting

### Service doesn't start

1. **Check Event Viewer** - Look for error details in Windows Event Viewer
2. **Verify credentials file** - Ensure `google-key.json` exists and is valid JSON
3. **Check permissions** - Service account needs read access to all files
4. **Test standalone** - Run `dotnet GoogleIndexingService.dll` directly to see errors

### URLs not being indexed

1. **Verify sitemap URL** - Test accessing it in browser
2. **Check credentials file** - Ensure it's valid and authorized for Indexing API
3. **Check Google API** - Verify Indexing API is enabled in Cloud Console
4. **Property access** - Ensure service account has access to property in Search Console
5. **Review logs** - Check `Logs/indexing-*.log` for error messages

### State file corruption

1. **Delete** `indexing-state.json` and restart service
2. **Service will create new state** and start from beginning

### Daily quota not resetting

1. Check `LastRunDate` in state file
2. Ensure system time is correct
3. If stuck, delete state file and restart

### Credentials file not found

- Ensure `google-key.json` is in the same directory as `GoogleIndexingService.exe`
- Check `AppSettings:GoogleCredentials` in appsettings.json matches the actual filename
- Verify file permissions allow the service account to read it

## Architecture

```
Services/
├── Interfaces/
│   ├── IIndexingServiceClient.cs    - Google Indexing API client
│   ├── IUrlProvider.cs              - Sitemap URL fetching
│   └── IIndexingStateStore.cs       - State persistence
├── IndexingServiceClient.cs         - Google API implementation
├── UrlProvider.cs                   - Sitemap parsing
└── IndexingStateStore.cs            - State file I/O

Models/
├── IndexingResult.cs                - Result enum (Success/Failure/QuotaExceeded)
└── IndexingState.cs                 - State model

Settings/
└── AppSettings.cs                   - Configuration model

Logging/
└── SerilogConfigurationExtensions.cs - Serilog setup

Worker.cs                            - Main service logic (BackgroundService)
Program.cs                           - DI and configuration setup
```

## Development

### Build

```bash
dotnet build
```

### Run Locally

```bash
dotnet run
```

## Performance

- **Network calls**: 1 per run (sitemap fetch) + 1 per URL (Google API)
- **Memory**: ~10-50 MB depending on sitemap size
- **Disk I/O**: Minimal (state file write only)
- **CPU**: Light processing (XML parsing, JSON serialization)

For 10,000 URL sitemaps with 200 quota: ~2-5 minutes per run

## License

This project is licensed under the [MIT License](LICENSE).

## Support

For issues, check:
1. Log files in `Logs/` directory
2. Google Cloud Console for API errors
3. Windows Event Viewer for service errors
4. Ensure credentials file is accessible
5. Verify sitemap URL is publicly accessible

---

© 2026-present [calKU0](https://github.com/calKU0)
