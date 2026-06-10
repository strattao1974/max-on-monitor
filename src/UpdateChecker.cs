using System.Net.Http;
using System.Text.Json;

namespace MaxOnMonitor;

internal static class UpdateChecker
{
    private const string ApiUrl =
        "https://api.github.com/repos/strattao1974/max-on-monitor/releases/latest";

    public const string ReleasesPage =
        "https://github.com/strattao1974/max-on-monitor/releases/latest";

    /// <summary>Returns the latest version and direct exe download URL, or null if unparseable.</summary>
    public static async Task<(Version Latest, string DownloadUrl)?> GetLatestReleaseAsync()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("MaxOnMonitor-UpdateCheck");

        using var doc = JsonDocument.Parse(await http.GetStringAsync(ApiUrl));
        string tag = doc.RootElement.GetProperty("tag_name").GetString() ?? "";

        string downloadUrl = ReleasesPage;
        if (doc.RootElement.TryGetProperty("assets", out var assets) && assets.GetArrayLength() > 0)
            downloadUrl = assets[0].GetProperty("browser_download_url").GetString() ?? ReleasesPage;

        return Version.TryParse(tag.TrimStart('v', 'V'), out var version)
            ? (version, downloadUrl)
            : null;
    }
}
