using BeatmapParser;
using BeatmapParser.HitObjects;
using BeatmapParser.TimingPoints;

namespace MapWizard.Tools.MapCleaner.Analysis;

public static class MapCleanerAnalyzer
{
    public static MapCleanerAnalysis Analyze(Beatmap beatmap)
    {
        var analysis = new MapCleanerAnalysis
        {
            BeatDivisorSignature = BuildBeatDivisorSignature(beatmap)
        };

        if (beatmap.TimingPoints == null || beatmap.TimingPoints.TimingPointList.Count == 0)
        {
            return analysis;
        }

        var timingPoints = beatmap.TimingPoints.TimingPointList
            .OrderBy(x => x.Time.TotalMilliseconds)
            .ToList();

        foreach (var slider in beatmap.HitObjects.Objects.OfType<Slider>())
        {
            AnalyzeRange(
                timingPoints,
                slider.Time.TotalMilliseconds,
                slider.EndTime.TotalMilliseconds,
                out var hasVolumeChange,
                out var hasSampleSetChange);

            if (hasVolumeChange)
            {
                analysis.UsesSliderVolumeChanges = true;
            }

            if (hasSampleSetChange)
            {
                analysis.UsesSliderSampleSetChanges = true;
            }

            if (analysis.UsesSliderVolumeChanges && analysis.UsesSliderSampleSetChanges)
            {
                break;
            }
        }

        foreach (var spinner in beatmap.HitObjects.Objects.OfType<Spinner>())
        {
            AnalyzeRange(
                timingPoints,
                spinner.Time.TotalMilliseconds,
                spinner.End.TotalMilliseconds,
                out var hasVolumeChange,
                out _);

            if (hasVolumeChange)
            {
                analysis.UsesSpinnerVolumeChanges = true;
                break;
            }
        }

        if (!analysis.UsesSpinnerVolumeChanges)
        {
            foreach (var hold in beatmap.HitObjects.Objects.OfType<ManiaHold>())
            {
                AnalyzeRange(
                    timingPoints,
                    hold.Time.TotalMilliseconds,
                    hold.End.TotalMilliseconds,
                    out var hasVolumeChange,
                    out _);

                if (!hasVolumeChange)
                {
                    continue;
                }

                analysis.UsesSpinnerVolumeChanges = true;
                break;
            }
        }

        return analysis;
    }

    private static string BuildBeatDivisorSignature(Beatmap beatmap)
    {
        if (beatmap.TimingPoints == null || beatmap.TimingPoints.TimingPointList.Count == 0)
        {
            return string.Empty;
        }

        var redlines = beatmap.TimingPoints.TimingPointList
            .OfType<UninheritedTimingPoint>()
            .OrderBy(x => x.Time.TotalMilliseconds)
            .ToList();

        if (redlines.Count == 0)
        {
            return string.Empty;
        }

        var usedDivisors = new HashSet<int>();

        foreach (var hitObject in beatmap.HitObjects.Objects)
        {
            RegisterDivisor(hitObject.Time.TotalMilliseconds, redlines, usedDivisors);

            switch (hitObject)
            {
                case Slider slider:
                    RegisterDivisor(slider.EndTime.TotalMilliseconds, redlines, usedDivisors);
                    break;
                case Spinner spinner:
                    RegisterDivisor(spinner.End.TotalMilliseconds, redlines, usedDivisors);
                    break;
                case ManiaHold hold:
                    RegisterDivisor(hold.End.TotalMilliseconds, redlines, usedDivisors);
                    break;
            }
        }

        if (usedDivisors.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(",", usedDivisors.OrderBy(x => x).Select(x => $"1/{x}"));
    }

    private static void RegisterDivisor(double timeMs, IReadOnlyList<UninheritedTimingPoint> redlines, ISet<int> usedDivisors)
    {
        var redline = redlines[0];
        foreach (var timingPoint in redlines)
        {
            if (timingPoint.Time.TotalMilliseconds > timeMs)
            {
                break;
            }

            redline = timingPoint;
        }

        var beatLength = Math.Abs(redline.BeatLength);
        if (beatLength <= 0.00001)
        {
            return;
        }

        var relative = (timeMs - redline.Time.TotalMilliseconds) / beatLength;
        var fraction = Math.Abs(relative - Math.Floor(relative));

        if (fraction < 0.0005 || Math.Abs(1.0 - fraction) < 0.0005)
        {
            return;
        }

        for (var denominator = 2; denominator <= 16; denominator++)
        {
            var scaled = fraction * denominator;
            var nearest = Math.Round(scaled, MidpointRounding.AwayFromZero);
            if (Math.Abs(scaled - nearest) > 0.005)
            {
                continue;
            }

            usedDivisors.Add(denominator);
            break;
        }
    }

    private static void AnalyzeRange(
        IReadOnlyList<TimingPoint> orderedTimingPoints,
        double start,
        double end,
        out bool hasVolumeChange,
        out bool hasSampleSetChange)
    {
        hasVolumeChange = false;
        hasSampleSetChange = false;

        for (var i = 0; i < orderedTimingPoints.Count; i++)
        {
            var timingPoint = orderedTimingPoints[i];
            var timingPointMs = timingPoint.Time.TotalMilliseconds;

            if (timingPointMs <= start || timingPointMs >= end)
            {
                continue;
            }

            var previous = FindPreviousTimingPoint(orderedTimingPoints, i, timingPointMs);
            if (previous == null)
            {
                continue;
            }

            if (timingPoint.Volume != previous.Volume)
            {
                hasVolumeChange = true;
            }

            if (timingPoint.SampleSet != previous.SampleSet || timingPoint.SampleIndex != previous.SampleIndex)
            {
                hasSampleSetChange = true;
            }

            if (hasVolumeChange && hasSampleSetChange)
            {
                return;
            }
        }
    }

    private static TimingPoint? FindPreviousTimingPoint(
        IReadOnlyList<TimingPoint> orderedTimingPoints,
        int currentIndex,
        double timeMs)
    {
        for (var i = currentIndex - 1; i >= 0; i--)
        {
            var timingPoint = orderedTimingPoints[i];
            if (timingPoint.Time.TotalMilliseconds <= timeMs)
            {
                return timingPoint;
            }
        }

        return null;
    }
}
