using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MapWizard.Desktop.Services;

internal static class GithubReleaseNotesService
{
    private const string ReleasesUrl = "https://api.github.com/repos/maotovisk/MapWizard/releases?per_page=30";
    private static readonly HttpClient Client = CreateClient();

    public static async Task<string?> GetNotesAsync(string version)
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            using var response = await Client.GetAsync(ReleasesUrl, timeout.Token);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: timeout.Token);

            foreach (var release in document.RootElement.EnumerateArray())
            {
                if (release.GetProperty("draft").GetBoolean() || !MatchesVersion(release, version))
                {
                    continue;
                }

                var notes = release.GetProperty("body").GetString();
                return string.IsNullOrWhiteSpace(notes) ? null : notes;
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException or JsonException)
        {
            // Release notes are optional; a GitHub API failure must not block the update.
        }

        return null;
    }

    private static bool MatchesVersion(JsonElement release, string version)
    {
        var name = release.GetProperty("name").GetString();
        return string.Equals(name, $"MapWizard pre-release {version}", StringComparison.OrdinalIgnoreCase)
               || string.Equals(name, $"MapWizard v{version}", StringComparison.OrdinalIgnoreCase);
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MapWizard");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }
}
