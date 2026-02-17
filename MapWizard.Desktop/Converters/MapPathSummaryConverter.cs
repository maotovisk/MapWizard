using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace MapWizard.Desktop.Converters;

public sealed class MapPathSummaryConverter : IValueConverter
{
    private static readonly Regex FullPattern =
        new(@"^(?<artist>.+?) - (?<title>.+?) \((?<mapper>.+?)\) \[(?<diff>.+?)\]$",
            RegexOptions.Compiled);

    private static readonly Regex ShortPattern =
        new(@"^(?<artist>.+?) - (?<title>.+?) \[(?<diff>.+?)\]$",
            RegexOptions.Compiled);

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var rawPath = value as string;
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            return "No map selected";
        }

        if (!IsValidOsuMapPath(rawPath))
        {
            return rawPath;
        }

        string fileName;
        try
        {
            fileName = Path.GetFileNameWithoutExtension(rawPath);
        }
        catch
        {
            fileName = rawPath;
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "No map selected";
        }

        var fullMatch = FullPattern.Match(fileName);
        if (fullMatch.Success)
        {
            var artist = fullMatch.Groups["artist"].Value.Trim();
            var title = fullMatch.Groups["title"].Value.Trim();
            var diff = fullMatch.Groups["diff"].Value.Trim();
            return $"{artist} - {title} [{diff}]";
        }

        var shortMatch = ShortPattern.Match(fileName);
        if (shortMatch.Success)
        {
            var artist = shortMatch.Groups["artist"].Value.Trim();
            var title = shortMatch.Groups["title"].Value.Trim();
            var diff = shortMatch.Groups["diff"].Value.Trim();
            return $"{artist} - {title} [{diff}]";
        }

        return rawPath;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return BindingOperations.DoNothing;
    }

    private static bool IsValidOsuMapPath(string path)
    {
        try
        {
            if (!path.EndsWith(".osu", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return File.Exists(Path.GetFullPath(path));
        }
        catch
        {
            return false;
        }
    }
}
