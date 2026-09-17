using System;
using System.Diagnostics;

namespace MapWizard.Desktop.Models;

/// <summary>
/// External links shown in the app (title bar, settings support tab).
/// </summary>
public static class AppLinks
{
    public const string Repository = "https://github.com/maotovisk/MapWizard";
    public const string Issues = "https://github.com/maotovisk/MapWizard/issues";
    public const string Website = "https://mapwizard.maot.dev";

    // Kept as the short link so the invite lives only in the Cloudflare redirect.
    public const string Discord = "https://mapwizard.maot.dev/discord";
    public const string Documentation = "https://mapwizard.maot.dev/#/wiki/Getting_started/en";

    /// <summary>
    /// Opens an http/https URL in the default browser. Returns false for invalid
    /// URLs or when the shell cannot launch the link.
    /// </summary>
    public static bool TryOpen(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = uri.ToString(),
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }
}
