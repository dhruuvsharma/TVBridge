using Microsoft.Extensions.Logging;

namespace TVBridge.Tunnel;

public sealed class CloudflaredDownloader
{
    private const string DownloadUrl =
        "https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe";

    private readonly ILogger<CloudflaredDownloader> _logger;

    public CloudflaredDownloader(ILogger<CloudflaredDownloader> logger)
    {
        _logger = logger;
    }

    public async Task<string> EnsureAvailableAsync(string targetPath, CancellationToken cancellationToken = default)
    {
        if (File.Exists(targetPath))
        {
            _logger.LogDebug("cloudflared found at {Path}", targetPath);
            return targetPath;
        }

        var directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        _logger.LogInformation("Downloading cloudflared to {Path}...", targetPath);

        using var httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromMinutes(5);

        var response = await httpClient.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var tempPath = targetPath + ".tmp";
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await stream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);

            File.Move(tempPath, targetPath, overwrite: true);
            _logger.LogInformation("cloudflared downloaded successfully");
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }

        return targetPath;
    }
}
