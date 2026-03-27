using BeatmapParser;
using BeatmapParser.HitObjects;
using BeatmapParser.TimingPoints;
using MapWizard.Tools.HelperExtensions;
using MapWizard.Tools.MapCleaner.Snapping;

namespace MapWizard.Tools.MapCleaner;

public static class MapCleaner
{
    public static MapCleanerResult CleanBeatmap(Beatmap beatmap, MapCleanerOptions? options = null)
    {
        options ??= new MapCleanerOptions();

        var result = new MapCleanerResult();
        var divisors = StableSnapEngine.ParseDivisors(options.SnapDivisors);
        var referenceBeatmap = Beatmap.Decode(beatmap.Encode());

        if (options.ResnapEverything)
        {
            result.TimingPointsResnapped += ResnapTimingPoints(beatmap, referenceBeatmap, divisors, options.ForwardRedlineWindowMs, ref result.GreenLinesResnapped);
            result.ObjectsResnapped += ResnapHitObjectStartTimes(beatmap, referenceBeatmap, divisors, options.ForwardRedlineWindowMs);
            ResnapHitObjectEndsAndSliderLengths(beatmap, referenceBeatmap, divisors, options, result);
            result.BookmarksResnapped += ResnapBookmarks(beatmap, referenceBeatmap, divisors, options.ForwardRedlineWindowMs);
            result.PreviewTimeResnapped += ResnapPreviewTime(beatmap, referenceBeatmap, divisors, options.ForwardRedlineWindowMs);
        }

        if (options.RemoveMuting)
        {
            result.MutedTimingPointsRestored += RemoveMuting(beatmap);
        }

        if (options.RemoveUnusedGreenlines)
        {
            result.GreenLinesRemoved += RemoveUnusedGreenlines(beatmap);
        }

        return result;
    }

    public static MapCleanerBatchResult CleanBeatmapTargets(string[] targetPaths, MapCleanerOptions? options = null)
    {
        options ??= new MapCleanerOptions();

        var batchResult = new MapCleanerBatchResult();

        foreach (var targetPath in targetPaths)
        {
            if (string.IsNullOrWhiteSpace(targetPath) || !File.Exists(targetPath))
            {
                batchResult.FailedBeatmaps++;
                batchResult.FailedPaths.Add(targetPath);
                batchResult.FailureDetails.Add($"'{targetPath}': File does not exist.");
                continue;
            }

            try
            {
                var beatmapText = File.ReadAllText(targetPath);
                var beatmap = Beatmap.Decode(beatmapText);
                var result = CleanBeatmap(beatmap, options);

                BeatmapBackupHelper.CreateBackupCopy(targetPath);
                File.WriteAllText(targetPath, beatmap.Encode().Replace("\r\n", "\n").Replace("\n", "\r\n"));

                batchResult.ProcessedBeatmaps++;
                batchResult.Add(result);
            }
            catch (Exception ex)
            {
                MapWizard.Tools.HelperExtensions.MapWizardLogger.LogException(ex);
                batchResult.FailedBeatmaps++;
                batchResult.FailedPaths.Add(targetPath);
                batchResult.FailureDetails.Add($"'{targetPath}': {ex.Message}");
            }
        }

        return batchResult;
    }

    private static int ResnapTimingPoints(
        Beatmap beatmap,
        Beatmap referenceBeatmap,
        IReadOnlyList<SnapDivisor> divisors,
        int forwardRedlineWindowMs,
        ref int greenLinesResnapped)
    {
        if (beatmap.TimingPoints == null || referenceBeatmap.TimingPoints == null)
        {
            return 0;
        }

        var timingPointsResnapped = 0;
        var referenceRedlines = referenceBeatmap.TimingPoints.TimingPointList
            .OfType<UninheritedTimingPoint>()
            .OrderBy(x => x.Time.TotalMilliseconds)
            .ToList();

        UninheritedTimingPoint? previousReferenceRedline = null;

        foreach (var timingPoint in beatmap.TimingPoints.TimingPointList)
        {
            var originalTime = timingPoint.Time.TotalMilliseconds;
            int snappedTime;

            if (timingPoint is UninheritedTimingPoint)
            {
                if (previousReferenceRedline == null)
                {
                    previousReferenceRedline = referenceRedlines.FirstOrDefault();
                    snappedTime = StableSnapEngine.StableRound(originalTime);
                }
                else
                {
                    snappedTime = StableSnapEngine.SnapRelativeMilliseconds(
                        originalTime,
                        previousReferenceRedline.Time.TotalMilliseconds,
                        previousReferenceRedline.BeatLength,
                        divisors);
                }

                var matchingReference = referenceRedlines
                    .LastOrDefault(x => x.Time.TotalMilliseconds <= originalTime);

                if (matchingReference != null)
                {
                    previousReferenceRedline = matchingReference;
                }
            }
            else
            {
                snappedTime = StableSnapEngine.SnapMilliseconds(
                    originalTime,
                    referenceBeatmap.TimingPoints,
                    divisors,
                    forwardRedlineWindowMs);
            }

            if (Math.Abs(snappedTime - originalTime) <= 0.0001)
            {
                continue;
            }

            timingPoint.Time = TimeSpan.FromMilliseconds(snappedTime);
            timingPointsResnapped++;

            if (timingPoint is InheritedTimingPoint)
            {
                greenLinesResnapped++;
            }
        }

        SortTimingPoints(beatmap);
        return timingPointsResnapped;
    }

    private static int ResnapHitObjectStartTimes(
        Beatmap beatmap,
        Beatmap referenceBeatmap,
        IReadOnlyList<SnapDivisor> divisors,
        int forwardRedlineWindowMs)
    {
        if (referenceBeatmap.TimingPoints == null)
        {
            return 0;
        }

        var objectsResnapped = 0;

        foreach (var hitObject in beatmap.HitObjects.Objects)
        {
            var originalTime = hitObject.Time.TotalMilliseconds;
            var snappedTime = StableSnapEngine.SnapMilliseconds(
                originalTime,
                referenceBeatmap.TimingPoints,
                divisors,
                forwardRedlineWindowMs);

            if (Math.Abs(snappedTime - originalTime) <= 0.0001)
            {
                continue;
            }

            hitObject.Time = TimeSpan.FromMilliseconds(snappedTime);
            objectsResnapped++;
        }

        return objectsResnapped;
    }

    private static void ResnapHitObjectEndsAndSliderLengths(
        Beatmap beatmap,
        Beatmap referenceBeatmap,
        IReadOnlyList<SnapDivisor> divisors,
        MapCleanerOptions options,
        MapCleanerResult result)
    {
        if (referenceBeatmap.TimingPoints == null)
        {
            return;
        }

        var referenceObjects = referenceBeatmap.HitObjects.Objects;

        for (var index = 0; index < beatmap.HitObjects.Objects.Count; index++)
        {
            var hitObject = beatmap.HitObjects.Objects[index];
            var referenceObject = index < referenceObjects.Count ? referenceObjects[index] : null;

            switch (hitObject)
            {
                case Slider slider when referenceObject is Slider referenceSlider:
                    if (ResnapSliderEndAndLength(beatmap, referenceBeatmap, slider, referenceSlider, divisors, options))
                    {
                        result.SliderEndsResnapped++;
                    }
                    break;
                case Spinner spinner when referenceObject is Spinner referenceSpinner:
                    if (ResnapSpinnerEnd(referenceBeatmap, spinner, referenceSpinner, divisors, options.ForwardRedlineWindowMs))
                    {
                        result.SpinnerOrHoldEndsResnapped++;
                    }
                    break;
                case ManiaHold maniaHold when referenceObject is ManiaHold referenceHold:
                    if (ResnapHoldEnd(referenceBeatmap, maniaHold, referenceHold, divisors, options.ForwardRedlineWindowMs))
                    {
                        result.SpinnerOrHoldEndsResnapped++;
                    }
                    break;
            }
        }
    }

    private static bool ResnapSliderEndAndLength(
        Beatmap beatmap,
        Beatmap referenceBeatmap,
        Slider slider,
        Slider referenceSlider,
        IReadOnlyList<SnapDivisor> divisors,
        MapCleanerOptions options)
    {
        var originalStart = referenceSlider.Time.TotalMilliseconds;
        var originalEnd = referenceSlider.EndTime.TotalMilliseconds;
        var originalDuration = Math.Max(1.0, originalEnd - originalStart);
        var slides = Math.Max(1, (int)slider.Slides);
        var snappedStart = slider.Time.TotalMilliseconds;

        var targetEnd = snappedStart + originalDuration;
        var snappedEnd = StableSnapEngine.SnapMilliseconds(
            targetEnd,
            referenceBeatmap.TimingPoints,
            divisors,
            options.ForwardRedlineWindowMs);

        if (snappedEnd <= snappedStart)
        {
            snappedEnd = StableSnapEngine.StableRound(snappedStart + 1);
        }

        var endChanged = Math.Abs(snappedEnd - slider.EndTime.TotalMilliseconds) > 0.0001;
        var newDuration = Math.Max(1.0, snappedEnd - snappedStart);
        var newLength = CalculateSliderLengthFromDuration(beatmap, slider, newDuration, slides);
        var lengthChanged = Math.Abs(newLength - slider.Length) > 0.0001;

        slider.EndTime = TimeSpan.FromMilliseconds(snappedEnd);
        slider.Length = newLength;

        return endChanged || lengthChanged;
    }

    private static bool ResnapSpinnerEnd(
        Beatmap referenceBeatmap,
        Spinner spinner,
        Spinner referenceSpinner,
        IReadOnlyList<SnapDivisor> divisors,
        int forwardRedlineWindowMs)
    {
        var originalDuration = Math.Max(1.0, referenceSpinner.End.TotalMilliseconds - referenceSpinner.Time.TotalMilliseconds);
        var targetEnd = spinner.Time.TotalMilliseconds + originalDuration;
        var snappedEnd = StableSnapEngine.SnapMilliseconds(
            targetEnd,
            referenceBeatmap.TimingPoints,
            divisors,
            forwardRedlineWindowMs);

        if (snappedEnd <= spinner.Time.TotalMilliseconds)
        {
            snappedEnd = StableSnapEngine.StableRound(spinner.Time.TotalMilliseconds + 1);
        }

        if (Math.Abs(snappedEnd - spinner.End.TotalMilliseconds) <= 0.0001)
        {
            return false;
        }

        spinner.End = TimeSpan.FromMilliseconds(snappedEnd);
        return true;
    }

    private static bool ResnapHoldEnd(
        Beatmap referenceBeatmap,
        ManiaHold maniaHold,
        ManiaHold referenceHold,
        IReadOnlyList<SnapDivisor> divisors,
        int forwardRedlineWindowMs)
    {
        var originalDuration = Math.Max(1.0, referenceHold.End.TotalMilliseconds - referenceHold.Time.TotalMilliseconds);
        var targetEnd = maniaHold.Time.TotalMilliseconds + originalDuration;
        var snappedEnd = StableSnapEngine.SnapMilliseconds(
            targetEnd,
            referenceBeatmap.TimingPoints,
            divisors,
            forwardRedlineWindowMs);

        if (snappedEnd <= maniaHold.Time.TotalMilliseconds)
        {
            snappedEnd = StableSnapEngine.StableRound(maniaHold.Time.TotalMilliseconds + 1);
        }

        if (Math.Abs(snappedEnd - maniaHold.End.TotalMilliseconds) <= 0.0001)
        {
            return false;
        }

        maniaHold.End = TimeSpan.FromMilliseconds(snappedEnd);
        return true;
    }

    private static double CalculateSliderLengthFromDuration(Beatmap beatmap, Slider slider, double durationMs, int slides)
    {
        var startMs = slider.Time.TotalMilliseconds;
        var uninheritedTimingPoint = beatmap.GetUninheritedTimingPointAt(startMs);
        var beatLength = Math.Abs(uninheritedTimingPoint?.BeatLength ?? 0);
        if (beatLength <= 0.00001)
        {
            beatLength = 1.0;
        }

        var inheritedTimingPoint = beatmap.GetInheritedTimingPointAt(startMs);
        var sliderVelocity = Math.Abs(inheritedTimingPoint?.SliderVelocity ?? 1.0);
        if (sliderVelocity <= 0.00001)
        {
            sliderVelocity = 1.0;
        }

        var sliderMultiplier = Math.Abs(beatmap.DifficultySection.SliderMultiplier);
        if (sliderMultiplier <= 0.00001)
        {
            sliderMultiplier = 1.0;
        }

        var length = (durationMs * sliderMultiplier * 100.0 * sliderVelocity) / (beatLength * slides);
        return double.IsFinite(length) && length > 0.00001 ? length : slider.Length;
    }

    private static int ResnapBookmarks(
        Beatmap beatmap,
        Beatmap referenceBeatmap,
        IReadOnlyList<SnapDivisor> divisors,
        int forwardRedlineWindowMs)
    {
        if (beatmap.Editor?.Bookmarks == null || referenceBeatmap.TimingPoints == null)
        {
            return 0;
        }

        var resnapped = 0;
        var bookmarks = beatmap.Editor.Bookmarks;

        for (var i = 0; i < bookmarks.Count; i++)
        {
            var originalTime = bookmarks[i].TotalMilliseconds;
            var snappedTime = StableSnapEngine.SnapMilliseconds(
                originalTime,
                referenceBeatmap.TimingPoints,
                divisors,
                forwardRedlineWindowMs);

            if (Math.Abs(snappedTime - originalTime) <= 0.0001)
            {
                continue;
            }

            bookmarks[i] = TimeSpan.FromMilliseconds(snappedTime);
            resnapped++;
        }

        beatmap.Editor.Bookmarks = bookmarks;
        return resnapped;
    }

    private static int ResnapPreviewTime(
        Beatmap beatmap,
        Beatmap referenceBeatmap,
        IReadOnlyList<SnapDivisor> divisors,
        int forwardRedlineWindowMs)
    {
        if (beatmap.GeneralSection.PreviewTime is not int previewTime || previewTime < 0 || referenceBeatmap.TimingPoints == null)
        {
            return 0;
        }

        var snappedPreviewTime = StableSnapEngine.SnapMilliseconds(
            previewTime,
            referenceBeatmap.TimingPoints,
            divisors,
            forwardRedlineWindowMs);

        if (snappedPreviewTime == previewTime)
        {
            return 0;
        }

        beatmap.GeneralSection.PreviewTime = snappedPreviewTime;
        return 1;
    }

    private static int RemoveMuting(Beatmap beatmap)
    {
        if (beatmap.TimingPoints == null)
        {
            return 0;
        }

        var restored = 0;
        var fallbackVolume = 100u;

        foreach (var timingPoint in beatmap.TimingPoints.TimingPointList.OrderBy(x => x.Time.TotalMilliseconds))
        {
            if (timingPoint.Volume <= 5)
            {
                timingPoint.Volume = fallbackVolume;
                restored++;
                continue;
            }

            fallbackVolume = timingPoint.Volume;
        }

        return restored;
    }

    private static int RemoveUnusedGreenlines(Beatmap beatmap)
    {
        if (beatmap.TimingPoints == null)
        {
            return 0;
        }

        SortTimingPoints(beatmap);
        var timingPoints = beatmap.TimingPoints.TimingPointList;
        var inheritedTimingPoints = timingPoints.OfType<InheritedTimingPoint>().ToList();
        if (inheritedTimingPoints.Count == 0)
        {
            return 0;
        }

        var usedGreenlines = GetUsedInheritedTimingPoints(beatmap, inheritedTimingPoints);
        var toRemove = new HashSet<TimingPoint>();

        foreach (var inheritedTimingPoint in inheritedTimingPoints)
        {
            if (!usedGreenlines.Contains(inheritedTimingPoint))
            {
                toRemove.Add(inheritedTimingPoint);
            }
        }

        var orderedTimingPoints = timingPoints
            .Select((timingPoint, index) => new IndexedTimingPoint(index, timingPoint))
            .OrderBy(x => x.TimingPoint.Time.TotalMilliseconds)
            .ThenBy(x => x.TimingPoint is UninheritedTimingPoint ? 0 : 1)
            .ThenBy(x => x.Index)
            .ToList();

        InheritedState? activeState = null;
        UninheritedTimingPoint? activeRedline = null;

        foreach (var (_, timingPoint) in orderedTimingPoints)
        {
            switch (timingPoint)
            {
                case UninheritedTimingPoint redline:
                    activeRedline = redline;
                    activeState = BuildBaselineInheritedState(redline);
                    break;
                case InheritedTimingPoint greenline:
                    var currentState = BuildInheritedState(greenline, activeRedline);
                    if (activeState != null && currentState.Equals(activeState))
                    {
                        toRemove.Add(greenline);
                    }
                    else
                    {
                        activeState = currentState;
                    }
                    break;
            }
        }

        if (toRemove.Count == 0)
        {
            return 0;
        }

        beatmap.TimingPoints.TimingPointList = timingPoints
            .Where(x => !toRemove.Contains(x))
            .ToList();
        SortTimingPoints(beatmap);
        return toRemove.Count;
    }

    private static HashSet<InheritedTimingPoint> GetUsedInheritedTimingPoints(
        Beatmap beatmap,
        IReadOnlyList<InheritedTimingPoint> inheritedTimingPoints)
    {
        var used = new HashSet<InheritedTimingPoint>();

        foreach (var hitObject in beatmap.HitObjects.Objects)
        {
            MarkActiveInheritedPointAt(beatmap, hitObject.Time.TotalMilliseconds, used);

            switch (hitObject)
            {
                case Slider slider:
                    MarkActiveInheritedPointAt(beatmap, slider.EndTime.TotalMilliseconds, used);

                    var slides = Math.Max(1, (int)slider.Slides);
                    for (var i = 1; i < slides; i++)
                    {
                        var repeatTime = slider.Time.TotalMilliseconds + ((slider.EndTime.TotalMilliseconds - slider.Time.TotalMilliseconds) * i / slides);
                        MarkActiveInheritedPointAt(beatmap, repeatTime, used);
                    }

                    foreach (var inheritedTimingPoint in inheritedTimingPoints)
                    {
                        var timeMs = inheritedTimingPoint.Time.TotalMilliseconds;
                        if (timeMs > slider.Time.TotalMilliseconds && timeMs <= slider.EndTime.TotalMilliseconds)
                        {
                            used.Add(inheritedTimingPoint);
                        }
                    }
                    break;
                case Spinner spinner:
                    MarkActiveInheritedPointAt(beatmap, spinner.End.TotalMilliseconds, used);
                    break;
                case ManiaHold maniaHold:
                    MarkActiveInheritedPointAt(beatmap, maniaHold.End.TotalMilliseconds, used);
                    break;
            }
        }

        return used;
    }

    private static void MarkActiveInheritedPointAt(Beatmap beatmap, double timeMs, ISet<InheritedTimingPoint> used)
    {
        if (beatmap.TimingPoints == null)
        {
            return;
        }

        InheritedTimingPoint? activeInherited = null;
        foreach (var timingPoint in beatmap.TimingPoints.TimingPointList
                     .OrderBy(x => x.Time.TotalMilliseconds)
                     .ThenBy(x => x is UninheritedTimingPoint ? 0 : 1))
        {
            if (timingPoint.Time.TotalMilliseconds > timeMs)
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

        if (activeInherited != null)
        {
            used.Add(activeInherited);
        }
    }

    private static InheritedState BuildBaselineInheritedState(UninheritedTimingPoint? redline)
    {
        return new InheritedState(
            sampleSet: redline?.SampleSet ?? 0,
            sampleIndex: redline?.SampleIndex ?? 0,
            volume: redline?.Volume ?? 100,
            sliderVelocity: 1.0,
            effectsSignature: GetEffectsSignature(redline?.Effects));
    }

    private static InheritedState BuildInheritedState(InheritedTimingPoint greenline, UninheritedTimingPoint? activeRedline)
    {
        return new InheritedState(
            sampleSet: greenline.SampleSet != 0 ? greenline.SampleSet : activeRedline?.SampleSet ?? 0,
            sampleIndex: greenline.SampleIndex != 0 ? greenline.SampleIndex : activeRedline?.SampleIndex ?? 0,
            volume: greenline.Volume != 0 ? greenline.Volume : activeRedline?.Volume ?? 100,
            sliderVelocity: greenline.SliderVelocity,
            effectsSignature: GetEffectsSignature(greenline.Effects));
    }

    private static string GetEffectsSignature(IEnumerable<BeatmapParser.Enums.Effect>? effects)
    {
        if (effects == null)
        {
            return string.Empty;
        }

        return string.Join(",", effects.OrderBy(x => (int)x).Select(x => x.ToString()));
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

    private sealed record IndexedTimingPoint(int Index, TimingPoint TimingPoint);

    private sealed record InheritedState(
        BeatmapParser.Enums.SampleSet sampleSet,
        uint sampleIndex,
        uint volume,
        double sliderVelocity,
        string effectsSignature)
    {
        public bool Equals(InheritedState? other)
        {
            if (other is null)
            {
                return false;
            }

            return sampleSet == other.sampleSet &&
                   sampleIndex == other.sampleIndex &&
                   volume == other.volume &&
                   Math.Abs(sliderVelocity - other.sliderVelocity) <= 0.0005 &&
                   effectsSignature == other.effectsSignature;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(sampleSet, sampleIndex, volume, Math.Round(sliderVelocity, 4), effectsSignature);
        }
    }
}
