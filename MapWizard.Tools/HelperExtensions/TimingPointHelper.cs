using BeatmapParser;
using BeatmapParser.HitObjects;
using BeatmapParser.Sections;
using BeatmapParser.TimingPoints;
using MapWizard.Tools.MapCleaner.Snapping;

namespace MapWizard.Tools.HelperExtensions;

public static class TimingPointHelper
{
    /// <summary>
    /// Removes redundant timing points from the beatmap.
    /// </summary>
    /// <param name="timingPointsSection"></param>
    /// <returns></returns>
    public static void RemoveRedundantGreenLines(this TimingPointsSection timingPointsSection)
    {
        var inheritedTimingPoints = timingPointsSection.TimingPointList.Where(x => x is InheritedTimingPoint).OrderBy(x => x.Time.TotalMilliseconds).ToList();
        
        var timingPointsToRemove = new List<TimingPoint>();

        foreach (var inheritedTimingPoint in inheritedTimingPoints)
        {
            
            var inheritedTimingPointIndex = timingPointsSection.TimingPointList.IndexOf(inheritedTimingPoint);
            
            if (inheritedTimingPointIndex == 0)
            {
                continue;
            }
            
            var previousTimingPoint = timingPointsSection.TimingPointList[inheritedTimingPointIndex - 1];
            
            if (previousTimingPoint is UninheritedTimingPoint or null)
            {
                continue;
            }

            previousTimingPoint = previousTimingPoint as InheritedTimingPoint;

            if (previousTimingPoint != null &&
                previousTimingPoint.SampleSet == inheritedTimingPoint.SampleSet &&
                previousTimingPoint.SampleIndex == inheritedTimingPoint.SampleIndex &&
                previousTimingPoint.Volume == inheritedTimingPoint.Volume &&
                Math.Abs(((InheritedTimingPoint)previousTimingPoint).SliderVelocity - ((InheritedTimingPoint)inheritedTimingPoint).SliderVelocity) < 0.0005 &&
                ((InheritedTimingPoint)previousTimingPoint).Effects == ((InheritedTimingPoint)inheritedTimingPoint).Effects)
            {   
                timingPointsToRemove.Add(inheritedTimingPoint);
            }
        }
        
        timingPointsSection.TimingPointList = timingPointsSection.TimingPointList.Except(timingPointsToRemove).ToList();
    }

    public static int ResnapGreenlinesToClosestAffectedPieces(
        this Beatmap beatmap,
        bool resnapWithoutAffectedPieces = false,
        IReadOnlyList<SnapDivisor>? divisors = null,
        int forwardRedlineWindowMs = 10)
    {
        if (beatmap.TimingPoints == null)
        {
            return 0;
        }

        SortTimingPoints(beatmap);
        var orderedTimingPoints = beatmap.TimingPoints.TimingPointList
            .OrderBy(x => x.Time.TotalMilliseconds)
            .ThenBy(x => x is UninheritedTimingPoint ? 0 : 1)
            .ToList();
        var inheritedTimingPoints = orderedTimingPoints.OfType<InheritedTimingPoint>().ToList();

        if (inheritedTimingPoints.Count == 0)
        {
            return 0;
        }

        var effectiveDivisors = divisors ?? StableSnapEngine.ParseDivisors(null);
        var volumeChangingGreenlines = GetVolumeChangingGreenlines(orderedTimingPoints);
        var closestCorePieceByGreenline = new Dictionary<InheritedTimingPoint, double>();
        var closestTickByGreenline = new Dictionary<InheritedTimingPoint, double>();

        foreach (var piece in EnumerateAffectedPieces(beatmap))
        {
            var activeGreenline = GetActiveInheritedTimingPointAt(orderedTimingPoints, piece.TimeMs);
            if (activeGreenline == null)
            {
                continue;
            }

            if (piece.IsSliderTick)
            {
                UpdateClosestAffectedPiece(closestTickByGreenline, activeGreenline, piece.TimeMs);
                continue;
            }

            UpdateClosestAffectedPiece(closestCorePieceByGreenline, activeGreenline, piece.TimeMs);
        }

        var resnapped = 0;
        foreach (var greenline in inheritedTimingPoints)
        {
            var hasCorePiece = closestCorePieceByGreenline.TryGetValue(greenline, out var closestCorePieceMs);
            var hasTick = closestTickByGreenline.TryGetValue(greenline, out var closestTickMs);
            var originalTime = greenline.Time.TotalMilliseconds;
            double targetTime;

            if (!hasCorePiece && !hasTick)
            {
                if (!resnapWithoutAffectedPieces)
                {
                    continue;
                }

                targetTime = StableSnapEngine.SnapMilliseconds(
                    originalTime,
                    beatmap.TimingPoints,
                    effectiveDivisors,
                    forwardRedlineWindowMs);
            }
            else
            {
                targetTime = hasCorePiece ? closestCorePieceMs : closestTickMs;
            }

            if (volumeChangingGreenlines.Contains(greenline) && hasTick)
            {
                if (!hasCorePiece)
                {
                    targetTime = closestTickMs;
                }
                else
                {
                    var coreDistance = Math.Abs(closestCorePieceMs - originalTime);
                    var tickDistance = Math.Abs(closestTickMs - originalTime);
                    if (tickDistance < coreDistance - 0.0001 ||
                        (Math.Abs(tickDistance - coreDistance) <= 0.0001 && closestTickMs < closestCorePieceMs))
                    {
                        targetTime = closestTickMs;
                    }
                }
            }

            if (resnapWithoutAffectedPieces)
            {
                targetTime = StableSnapEngine.SnapMilliseconds(
                    targetTime,
                    beatmap.TimingPoints,
                    effectiveDivisors,
                    forwardRedlineWindowMs);
            }

            if (Math.Abs(targetTime - originalTime) <= 0.0001)
            {
                continue;
            }

            greenline.Time = TimeSpan.FromMilliseconds(targetTime);
            resnapped++;
        }

        if (resnapped > 0)
        {
            SortTimingPoints(beatmap);
        }

        return resnapped;
    }

    private static HashSet<InheritedTimingPoint> GetVolumeChangingGreenlines(IReadOnlyList<TimingPoint> orderedTimingPoints)
    {
        var volumeChangingGreenlines = new HashSet<InheritedTimingPoint>();
        var activeVolume = 100u;
        UninheritedTimingPoint? activeRedline = null;

        foreach (var timingPoint in orderedTimingPoints)
        {
            switch (timingPoint)
            {
                case UninheritedTimingPoint redline:
                    activeRedline = redline;
                    activeVolume = redline.Volume;
                    break;
                case InheritedTimingPoint greenline:
                    var effectiveVolume = greenline.Volume != 0 ? greenline.Volume : activeRedline?.Volume ?? activeVolume;
                    if (effectiveVolume != activeVolume)
                    {
                        volumeChangingGreenlines.Add(greenline);
                    }

                    activeVolume = effectiveVolume;
                    break;
            }
        }

        return volumeChangingGreenlines;
    }

    private static void UpdateClosestAffectedPiece(
        IDictionary<InheritedTimingPoint, double> closestByGreenline,
        InheritedTimingPoint greenline,
        double candidatePieceTimeMs)
    {
        var roundedCandidatePieceTimeMs = StableSnapEngine.StableFloor(candidatePieceTimeMs);

        if (!closestByGreenline.TryGetValue(greenline, out var currentClosestPieceTimeMs))
        {
            closestByGreenline[greenline] = roundedCandidatePieceTimeMs;
            return;
        }

        var greenlineTime = greenline.Time.TotalMilliseconds;
        var currentDistance = Math.Abs(currentClosestPieceTimeMs - greenlineTime);
        var candidateDistance = Math.Abs(roundedCandidatePieceTimeMs - greenlineTime);

        if (candidateDistance < currentDistance - 0.0001 ||
            (Math.Abs(candidateDistance - currentDistance) <= 0.0001 && roundedCandidatePieceTimeMs < currentClosestPieceTimeMs))
        {
            closestByGreenline[greenline] = roundedCandidatePieceTimeMs;
        }
    }

    private static IEnumerable<AffectedPiece> EnumerateAffectedPieces(Beatmap beatmap)
    {
        foreach (var hitObject in beatmap.HitObjects.Objects)
        {
            switch (hitObject)
            {
                case Circle circle:
                    yield return new AffectedPiece(circle.Time.TotalMilliseconds, IsSliderTick: false);
                    break;
                case Slider slider:
                {
                    var startMs = slider.Time.TotalMilliseconds;
                    var endMs = slider.EndTime.TotalMilliseconds;
                    yield return new AffectedPiece(startMs, IsSliderTick: false);

                    var slides = Math.Max(1, (int)slider.Slides);
                    for (var i = 1; i <= slides; i++)
                    {
                        var edgeTime = startMs + ((endMs - startMs) * i / slides);
                        yield return new AffectedPiece(edgeTime, IsSliderTick: false);
                    }

                    foreach (var tickTime in EnumerateSliderTickTimes(beatmap, slider, startMs, endMs, slides))
                    {
                        yield return new AffectedPiece(tickTime, IsSliderTick: true);
                    }

                    break;
                }
                case Spinner spinner:
                    yield return new AffectedPiece(spinner.End.TotalMilliseconds, IsSliderTick: false);
                    break;
            }
        }
    }

    private static IEnumerable<double> EnumerateSliderTickTimes(
        Beatmap beatmap,
        Slider slider,
        double startMs,
        double endMs,
        int slides)
    {
        if (slides <= 0 || endMs <= startMs + 0.0001)
        {
            yield break;
        }

        var sliderTickRate = Math.Abs(beatmap.DifficultySection.SliderTickRate);
        if (sliderTickRate <= 0.00001)
        {
            yield break;
        }

        var sliderVelocity = Math.Abs(beatmap.GetInheritedTimingPointAt(startMs)?.SliderVelocity ?? 1.0);
        if (sliderVelocity <= 0.00001)
        {
            sliderVelocity = 1.0;
        }

        var sliderMultiplier = Math.Abs(beatmap.DifficultySection.SliderMultiplier);
        if (sliderMultiplier <= 0.00001)
        {
            sliderMultiplier = 1.0;
        }

        var sliderLength = Math.Abs(slider.Length);
        if (sliderLength <= 0.00001)
        {
            yield break;
        }

        var scoringDistance = sliderMultiplier * 100.0 * sliderVelocity;
        if (scoringDistance <= 0.00001)
        {
            yield break;
        }

        var tickDistance = scoringDistance / sliderTickRate;
        if (tickDistance <= 0.00001)
        {
            yield break;
        }

        var spanDuration = (endMs - startMs) / slides;
        if (spanDuration <= 0.00001)
        {
            yield break;
        }

        for (var spanIndex = 0; spanIndex < slides; spanIndex++)
        {
            var spanStart = startMs + (spanDuration * spanIndex);

            for (var distance = tickDistance; distance < sliderLength - 0.01; distance += tickDistance)
            {
                var tickProgress = distance / sliderLength;
                var tickTime = spanStart + (spanDuration * tickProgress);
                yield return tickTime;
            }
        }
    }

    private static InheritedTimingPoint? GetActiveInheritedTimingPointAt(IReadOnlyList<TimingPoint> orderedTimingPoints, double timeMs)
    {
        InheritedTimingPoint? activeInherited = null;

        foreach (var timingPoint in orderedTimingPoints)
        {
            if (timingPoint.Time.TotalMilliseconds > timeMs + 0.0001)
            {
                break;
            }

            switch (timingPoint)
            {
                case UninheritedTimingPoint:
                    activeInherited = null;
                    break;
                case InheritedTimingPoint inheritedTimingPoint:
                    activeInherited = inheritedTimingPoint;
                    break;
            }
        }

        return activeInherited;
    }

    private static void SortTimingPoints(Beatmap beatmap)
    {
        if (beatmap.TimingPoints == null)
        {
            return;
        }

        beatmap.TimingPoints.TimingPointList = beatmap.TimingPoints.TimingPointList
            .Select((timingPoint, index) => new IndexedTimingPoint(index, timingPoint))
            .OrderBy(x => x.TimingPoint.Time.TotalMilliseconds)
            .ThenBy(x => x.TimingPoint is UninheritedTimingPoint ? 0 : 1)
            .ThenBy(x => x.Index)
            .Select(x => x.TimingPoint)
            .ToList();
    }

    private readonly record struct AffectedPiece(double TimeMs, bool IsSliderTick);

    private readonly record struct IndexedTimingPoint(int Index, TimingPoint TimingPoint);
}
