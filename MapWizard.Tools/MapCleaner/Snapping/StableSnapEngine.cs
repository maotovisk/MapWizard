using BeatmapParser.Sections;
using BeatmapParser.TimingPoints;

namespace MapWizard.Tools.MapCleaner.Snapping;

public static class StableSnapEngine
{
    public static IReadOnlyList<SnapDivisor> ParseDivisors(IEnumerable<string>? rawDivisors)
    {
        var divisors = new List<SnapDivisor>();

        if (rawDivisors != null)
        {
            foreach (var rawDivisor in rawDivisors)
            {
                if (string.IsNullOrWhiteSpace(rawDivisor))
                {
                    continue;
                }

                var cleaned = rawDivisor.Trim();
                var split = cleaned.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                if (split.Length != 2)
                {
                    continue;
                }

                if (!int.TryParse(split[0], out var numerator) || !int.TryParse(split[1], out var denominator))
                {
                    continue;
                }

                if (numerator <= 0 || denominator <= 0)
                {
                    continue;
                }

                divisors.Add(new SnapDivisor(numerator, denominator));
            }
        }

        if (divisors.Count == 0)
        {
            return [new SnapDivisor(1, 8), new SnapDivisor(1, 12)];
        }

        return divisors
            .Distinct()
            .OrderBy(d => d.Denominator)
            .ThenBy(d => d.Numerator)
            .ToList();
    }

    public static int SnapMilliseconds(
        double objectTimeMs,
        TimingPointsSection? timingPoints,
        IReadOnlyList<SnapDivisor> divisors,
        int forwardRedlineWindowMs)
    {
        if (timingPoints == null || timingPoints.TimingPointList.Count == 0 || divisors.Count == 0)
        {
            return StableFloor(objectTimeMs);
        }

        var redlines = timingPoints.TimingPointList
            .OfType<UninheritedTimingPoint>()
            .OrderBy(tp => tp.Time.TotalMilliseconds)
            .ToList();

        if (redlines.Count == 0)
        {
            return StableFloor(objectTimeMs);
        }

        var previousRedline = redlines[0];
        UninheritedTimingPoint? nextRedline = null;
        foreach (var redline in redlines)
        {
            var redlineMs = redline.Time.TotalMilliseconds;
            if (redlineMs <= objectTimeMs)
            {
                previousRedline = redline;
                continue;
            }

            nextRedline = redline;
            break;
        }

        var snapped = SnapRelativeMillisecondsRaw(
            objectTimeMs,
            previousRedline.Time.TotalMilliseconds,
            previousRedline.BeatLength,
            divisors);

        if (nextRedline != null &&
            snapped > previousRedline.Time.TotalMilliseconds + forwardRedlineWindowMs &&
            snapped >= nextRedline.Time.TotalMilliseconds - forwardRedlineWindowMs)
        {
            snapped = nextRedline.Time.TotalMilliseconds;
        }

        return StableFloor(snapped);
    }

    public static int SnapRelativeMilliseconds(
        double objectTimeMs,
        double anchorTimeMs,
        double beatLength,
        IReadOnlyList<SnapDivisor> divisors)
    {
        return StableFloor(SnapRelativeMillisecondsRaw(objectTimeMs, anchorTimeMs, beatLength, divisors));
    }

    public static int StableRound(double value)
    {
        if (value >= 0)
        {
            return (int)Math.Floor(value + 0.5);
        }

        return (int)Math.Ceiling(value - 0.5);
    }

    public static int StableFloor(double value)
    {
        return (int)Math.Floor(value);
    }

    private static double SnapRelativeMillisecondsRaw(
        double objectTimeMs,
        double anchorTimeMs,
        double beatLength,
        IReadOnlyList<SnapDivisor> divisors)
    {
        if (divisors.Count == 0 || Math.Abs(beatLength) < 0.00001)
        {
            return objectTimeMs;
        }

        var snappedTime = 0d;
        var lowestDistance = double.PositiveInfinity;

        foreach (var divisor in divisors)
        {
            var step = Math.Abs(beatLength) * divisor.Numerator / divisor.Denominator;
            if (step <= 0.00001)
            {
                continue;
            }

            var candidate = GetNearestTick(objectTimeMs, anchorTimeMs, step);
            var distance = Math.Abs(objectTimeMs - candidate);
            if (distance < lowestDistance)
            {
                lowestDistance = distance;
                snappedTime = candidate;
            }
        }

        return double.IsPositiveInfinity(lowestDistance) ? objectTimeMs : snappedTime;
    }

    private static double GetNearestTick(double timeMs, double anchorTimeMs, double step)
    {
        var remainder = (timeMs - anchorTimeMs) % step;
        if (remainder < 0.5 * step)
        {
            return timeMs - remainder;
        }

        return timeMs - remainder + step;
    }
}
