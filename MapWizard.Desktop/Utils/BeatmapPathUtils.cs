using System;
using System.IO;

namespace MapWizard.Desktop.Utils;

public static class BeatmapPathUtils
{
    public static string? TryGetMapsetDirectoryPath(string? beatmapPath)
    {
        if (string.IsNullOrWhiteSpace(beatmapPath))
        {
            return null;
        }

        try
        {
            return Path.GetDirectoryName(Path.GetFullPath(beatmapPath));
        }
        catch
        {
            return null;
        }
    }
}
