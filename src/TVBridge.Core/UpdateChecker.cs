using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace TVBridge.Core;

/// <summary>
/// Checks for new releases on GitHub. No telemetry, no auto-download.
/// Simply compares the current version against the latest GitHub Release tag.
/// </summary>
public sealed class UpdateChecker
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UpdateChecker> _logger;

    public static string CurrentVersion => "1.0.0";

    public UpdateChecker(HttpClient httpClient, ILogger<UpdateChecker> logger)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("TVBridge-UpdateChecker/1.0");
        _logger = logger;
    }

    /// <summary>
    /// Check if a newer version is available on GitHub.
    /// Returns the new version string if available, null if up to date or check fails.
    /// </summary>
    public async Task<UpdateInfo?> CheckAsync(
        string owner = "dhruuvsharma",
        string repo = "TVBridge",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"https://api.github.com/repos/{owner}/{repo}/releases/latest";
            var release = await _httpClient.GetFromJsonAsync<GitHubRelease>(url, cancellationToken)
                .ConfigureAwait(false);

            if (release is null || string.IsNullOrWhiteSpace(release.TagName))
                return null;

            var latestVersion = release.TagName.TrimStart('v');
            if (!Version.TryParse(latestVersion, out var latest) ||
                !Version.TryParse(CurrentVersion, out var current))
                return null;

            if (latest > current)
            {
                _logger.LogInformation("New version available: {Version} (current: {Current})",
                    latestVersion, CurrentVersion);
                return new UpdateInfo(latestVersion, release.HtmlUrl ?? "", release.Body ?? "");
            }

            _logger.LogDebug("TVBridge is up to date ({Version})", CurrentVersion);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Update check failed (non-critical)");
            return null;
        }
    }
}

public sealed record UpdateInfo(string Version, string Url, string ReleaseNotes);

internal sealed record GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string? TagName { get; init; }

    [JsonPropertyName("html_url")]
    public string? HtmlUrl { get; init; }

    [JsonPropertyName("body")]
    public string? Body { get; init; }
}
