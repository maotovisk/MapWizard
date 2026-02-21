using BeatmapParser;
using BeatmapParser.TimingPoints;

namespace MapWizard.Tools.MapCleaner.Timing;

public static class InheritedPruner
{
    public static int PruneUnusedInheritedTimingPoints(Beatmap beatmap, MapCleanerAnalysis analysis)
    {
        var section = beatmap.TimingPoints;
        if (section == null || section.TimingPointList.Count == 0)
        {
            return 0;
        }

        var originalTimingPoints = section.TimingPointList.ToList();
        var requiredTimes = TimingInfluenceRebuilder.BuildRequiredTimes(
            beatmap,
            includeSliderBodyTiming: analysis.UsesSliderVolumeChanges || analysis.UsesSliderSampleSetChanges,
            includeSpinnerBodyTiming: analysis.UsesSpinnerVolumeChanges);

        if (requiredTimes.Count == 0)
        {
            return 0;
        }

        var ordered = originalTimingPoints
            .Select((tp, idx) => new IndexedTimingPoint(idx, tp))
            .OrderBy(x => x.TimingPoint.Time.TotalMilliseconds)
            .ThenBy(x => x.Index)
            .ToList();

        var activeInheritedIndices = new HashSet<int>();

        foreach (var time in requiredTimes)
        {
            var active = FindActiveTimingPoint(ordered, time);
            if (active?.TimingPoint is InheritedTimingPoint)
            {
                activeInheritedIndices.Add(active.Index);
            }
        }

        var filtered = originalTimingPoints
            .Select((timingPoint, index) => new IndexedTimingPoint(index, timingPoint))
            .Where(x => x.TimingPoint is not InheritedTimingPoint || activeInheritedIndices.Contains(x.Index))
            .OrderBy(x => x.TimingPoint.Time.TotalMilliseconds)
            .ThenBy(x => x.Index)
            .ToList();

        var cleaned = new List<TimingPoint>(filtered.Count);

        foreach (var item in filtered)
        {
            if (cleaned.Count > 0 && cleaned[^1] is InheritedTimingPoint previousInherited &&
                item.TimingPoint is InheritedTimingPoint currentInherited &&
                AreEquivalentInherited(previousInherited, currentInherited))
            {
                continue;
            }

            cleaned.Add(item.TimingPoint);
        }

        var removed = originalTimingPoints.Count - cleaned.Count;
        section.TimingPointList = cleaned;
        beatmap.TimingPoints = section;

        return removed;
    }

    private static IndexedTimingPoint? FindActiveTimingPoint(IReadOnlyList<IndexedTimingPoint> orderedTimingPoints, double time)
    {
        IndexedTimingPoint? active = null;

        foreach (var timingPoint in orderedTimingPoints)
        {
            if (timingPoint.TimingPoint.Time.TotalMilliseconds > time)
            {
                break;
            }

            active = timingPoint;
        }

        return active;
    }

    private static bool AreEquivalentInherited(InheritedTimingPoint previous, InheritedTimingPoint current)
    {
        if (previous.SampleSet != current.SampleSet ||
            previous.SampleIndex != current.SampleIndex ||
            previous.Volume != current.Volume)
        {
            return false;
        }

        if (Math.Abs(previous.SliderVelocity - current.SliderVelocity) > 0.0005)
        {
            return false;
        }

        if (previous.Effects.Count != current.Effects.Count)
        {
            return false;
        }

        for (var i = 0; i < previous.Effects.Count; i++)
        {
            if (previous.Effects[i] != current.Effects[i])
            {
                return false;
            }
        }

        return true;
    }

    private sealed record IndexedTimingPoint(int Index, TimingPoint TimingPoint);
}
