using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MapWizard.Desktop.Utils;

public static class MillisecondParser
{
    private static readonly Regex OsuTimestampRegex = new(
        @"(?<min>\d{1,4}):(?<sec>\d{1,2}):(?<ms>\d{1,3})",
        RegexOptions.Compiled);

    public static bool TryParseMillisecondInput(string? input, out double value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var normalized = input.Trim();
        if (normalized.EndsWith("ms", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[..^2].Trim();
        }

        if (TryParseOsuTimestamp(normalized, out value))
        {
            return true;
        }

        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
               || double.TryParse(normalized, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
    }

    private static bool TryParseOsuTimestamp(string input, out double value)
    {
        value = 0;
        var match = OsuTimestampRegex.Match(input);
        if (!match.Success)
        {
            return false;
        }

        if (!int.TryParse(match.Groups["min"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var minutes) ||
            !int.TryParse(match.Groups["sec"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) ||
            !int.TryParse(match.Groups["ms"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var milliseconds))
        {
            return false;
        }

        if (seconds is < 0 or > 59 || milliseconds is < 0 or > 999)
        {
            return false;
        }

        value = minutes * 60_000d + seconds * 1_000d + milliseconds;
        return true;
    }
}
