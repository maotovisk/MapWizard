using System;
using System.IO;
using System.Threading;

namespace MapWizard.Desktop.Utils;

internal readonly record struct BeatmapCardInfo(
    string Artist,
    string ArtistUnicode,
    string Title,
    string TitleUnicode,
    string Creator,
    string Version,
    string? BackgroundFilename);

internal static class BeatmapCardInfoReader
{
    public static BeatmapCardInfo Read(string path, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(path);
        var section = string.Empty;
        var sawMetadata = false;
        var artist = string.Empty;
        var artistUnicode = string.Empty;
        var title = string.Empty;
        var titleUnicode = string.Empty;
        var creator = string.Empty;
        var version = string.Empty;
        string? backgroundFilename = null;
        var lineNumber = 0;

        while (reader.ReadLine() is { } line)
        {
            if ((++lineNumber & 255) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            var text = line.AsSpan().Trim();
            if (text.IsEmpty || text.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            if (text[0] == '[' && text[^1] == ']')
            {
                var nextSection = text[1..^1].ToString();
                if (sawMetadata && section.Equals("Events", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                section = nextSection;
                sawMetadata |= section.Equals("Metadata", StringComparison.OrdinalIgnoreCase);
                if (section.Equals("HitObjects", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                continue;
            }

            if (section.Equals("Metadata", StringComparison.OrdinalIgnoreCase))
            {
                var colon = text.IndexOf(':');
                if (colon < 0)
                {
                    continue;
                }

                var key = text[..colon].Trim();
                var value = text[(colon + 1)..].Trim().ToString();
                if (key.Equals("Artist", StringComparison.OrdinalIgnoreCase)) artist = value;
                else if (key.Equals("ArtistUnicode", StringComparison.OrdinalIgnoreCase)) artistUnicode = value;
                else if (key.Equals("Title", StringComparison.OrdinalIgnoreCase)) title = value;
                else if (key.Equals("TitleUnicode", StringComparison.OrdinalIgnoreCase)) titleUnicode = value;
                else if (key.Equals("Creator", StringComparison.OrdinalIgnoreCase)) creator = value;
                else if (key.Equals("Version", StringComparison.OrdinalIgnoreCase)) version = value;
            }
            else if (section.Equals("Events", StringComparison.OrdinalIgnoreCase) &&
                     backgroundFilename is null && text.StartsWith("0,0,", StringComparison.Ordinal))
            {
                var filename = text[4..].TrimStart();
                if (!filename.IsEmpty && filename[0] == '"')
                {
                    var endQuote = filename[1..].IndexOf('"');
                    backgroundFilename = endQuote < 0 ? null : filename.Slice(1, endQuote).ToString();
                }
                else
                {
                    var end = filename.IndexOf(',');
                    var candidate = (end < 0 ? filename : filename[..end]).Trim();
                    backgroundFilename = candidate.IsEmpty ? null : candidate.ToString();
                }
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        return new BeatmapCardInfo(artist, artistUnicode, title, titleUnicode, creator, version, backgroundFilename);
    }
}
