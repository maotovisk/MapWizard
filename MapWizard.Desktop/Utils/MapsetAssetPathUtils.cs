using System.IO;

namespace MapWizard.Desktop.Utils;

public static class MapsetAssetPathUtils
{
    public static string? ResolveRelativePath(string mapsetDirectory, string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(mapsetDirectory) || string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var sanitized = relativePath.Trim()
            .Trim('"')
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);

        var candidate = Path.IsPathRooted(sanitized)
            ? sanitized
            : Path.Combine(mapsetDirectory, sanitized);

        return File.Exists(candidate) ? candidate : null;
    }

    public static string? ResolveRelativePathFromBeatmap(string beatmapPath, string? relativePath)
    {
        var mapsetDirectory = BeatmapPathUtils.TryGetMapsetDirectoryPath(beatmapPath);
        return mapsetDirectory is null ? null : ResolveRelativePath(mapsetDirectory, relativePath);
    }
}
