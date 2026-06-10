using System.Net.Http;
using System.Text.Json;

namespace MaxOnMonitor;

internal static class UpdateChecker
{
    private const string ApiUrl =
        "https://api.github.com/repos/strattao1974/max-on-monitor/releases/latest";

    public const string ReleasesPage =
        "https://github.com/strattao1974/max-on-monitor/releases/latest";

    /// <summary>Returns the latest released version and its page URL, or null if the tag isn't parseable.</summary>
    public static async Task<(Version Latest, string Url)?> GetLatestReleaseAsync()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("MaxOnMonitor-UpdateCheck");

        using var doc = JsonDocument.Parse(await http.GetStringAsync(ApiUrl));
        string tag = doc.RootElement.GetProperty("tag_name").GetString() ?? "";
        string url = doc.RootElement.TryGetProperty("html_url", out var u)
            ? u.GetString() ?? ReleasesPage
            : ReleasesPage;

        return Version.TryParse(tag.TrimStart('v', 'V'), out var version)
            ? (version, url)
            : null;
    }
}
