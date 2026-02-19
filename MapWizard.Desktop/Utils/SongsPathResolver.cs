using MapWizard.Desktop.Services;

namespace MapWizard.Desktop.Utils;

public static class SongsPathResolver
{
    public static string ResolveSongsPath(ISettingsService settingsService, ISongLibraryService songLibraryService)
    {
        var settings = settingsService.GetMainSettings();
        var configuredPath = settings.SongsPath;
        if (songLibraryService.IsValidSongsPath(configuredPath))
        {
            return configuredPath;
        }

        var detectedPath = songLibraryService.TryDetectSongsPath();
        if (songLibraryService.IsValidSongsPath(detectedPath))
        {
            settings.SongsPath = detectedPath!;
            settingsService.SaveMainSettings(settings);
            return detectedPath!;
        }

        return string.Empty;
    }
}
