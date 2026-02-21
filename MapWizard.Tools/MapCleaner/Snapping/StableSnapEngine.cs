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
            return StableRound(objectTimeMs);
        }

        var redlines = timingPoints.TimingPointList
            .OfType<UninheritedTimingPoint>()
            .OrderBy(tp => tp.Time.TotalMilliseconds)
            .ToList();

        if (redlines.Count == 0)
        {
            return StableRound(objectTimeMs);
        }

        var snapTimingPoint = ResolveSnapRedline(redlines, objectTimeMs, forwardRedlineWindowMs);
        var redlineTime = snapTimingPoint.Time.TotalMilliseconds;
        var beatLength = snapTimingPoint.BeatLength;

        if (Math.Abs(beatLength) < 0.00001)
        {
            return StableRound(objectTimeMs);
        }

        var best = StableRound(objectTimeMs);
        var bestDistance = double.MaxValue;

        foreach (var divisor in divisors)
        {
            var step = Math.Abs(beatLength) * divisor.Numerator / divisor.Denominator;
            if (step <= 0.00001)
            {
                continue;
            }

            var relativeStep = (objectTimeMs - redlineTime) / step;
            var nearestStep = (int)Math.Round(relativeStep, MidpointRounding.AwayFromZero);

            for (var offset = -1; offset <= 1; offset++)
            {
                var candidateTime = redlineTime + ((nearestStep + offset) * step);
                var candidateRounded = StableRound(candidateTime);
                var candidateDistance = Math.Abs(candidateRounded - objectTimeMs);

                if (candidateDistance < bestDistance)
                {
                    best = candidateRounded;
                    bestDistance = candidateDistance;
                    continue;
                }

                if (Math.Abs(candidateDistance - bestDistance) < 0.00001)
                {
                    // Favor earlier snaps to reduce accidental forward drift.
                    if (candidateRounded < best)
                    {
                        best = candidateRounded;
                    }
                }
            }
        }

        return bestDistance == double.MaxValue ? StableRound(objectTimeMs) : best;
    }

    public static int StableRound(double value)
    {
        if (value >= 0)
        {
            return (int)Math.Floor(value + 0.5);
        }

        return (int)Math.Ceiling(value - 0.5);
    }

    private static UninheritedTimingPoint ResolveSnapRedline(
        IReadOnlyList<UninheritedTimingPoint> redlines,
        double objectTimeMs,
        int forwardRedlineWindowMs)
    {
        var previous = redlines[0];

        foreach (var redline in redlines)
        {
            var redlineMs = redline.Time.TotalMilliseconds;
            if (redlineMs <= objectTimeMs)
            {
                previous = redline;
                continue;
            }

            if (redlineMs - objectTimeMs <= forwardRedlineWindowMs)
            {
                return redline;
            }

            break;
        }

        return previous;
    }
}
