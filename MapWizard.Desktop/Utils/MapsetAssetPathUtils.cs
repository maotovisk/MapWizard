using System;
using System.IO;
using System.Linq;

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

        if (File.Exists(candidate))
        {
            return candidate;
        }

        if (Path.IsPathRooted(sanitized))
        {
            return null;
        }

        return TryResolveCaseInsensitivePath(mapsetDirectory, sanitized)
               ?? TryResolveByFilename(mapsetDirectory, sanitized);
    }

    public static string? ResolveRelativePathFromBeatmap(string beatmapPath, string? relativePath)
    {
        var mapsetDirectory = BeatmapPathUtils.TryGetMapsetDirectoryPath(beatmapPath);
        return mapsetDirectory is null ? null : ResolveRelativePath(mapsetDirectory, relativePath);
    }

    private static string? TryResolveCaseInsensitivePath(string mapsetDirectory, string relativePath)
    {
        try
        {
            var current = Path.GetFullPath(mapsetDirectory);
            var segments = relativePath
                .Split([Path.DirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length == 0)
            {
                return null;
            }

            foreach (var segment in segments)
            {
                var match = Directory.EnumerateFileSystemEntries(current)
                    .FirstOrDefault(entry =>
                        string.Equals(Path.GetFileName(entry), segment, StringComparison.OrdinalIgnoreCase));

                if (string.IsNullOrWhiteSpace(match))
                {
                    return null;
                }

                current = match;
            }

            return File.Exists(current) ? current : null;
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            return null;
        }
    }

    private static string? TryResolveByFilename(string mapsetDirectory, string relativePath)
    {
        var fileName = Path.GetFileName(relativePath);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        try
        {
            var direct = Directory.EnumerateFiles(mapsetDirectory, "*", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(file => string.Equals(Path.GetFileName(file), fileName, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(direct))
            {
                return direct;
            }

            return Directory.EnumerateFiles(mapsetDirectory, "*", SearchOption.AllDirectories)
                .FirstOrDefault(file => string.Equals(Path.GetFileName(file), fileName, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
            return null;
        }
    }
}
