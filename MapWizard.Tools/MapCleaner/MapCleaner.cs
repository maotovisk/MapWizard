using BeatmapParser;
using BeatmapParser.HitObjects;
using BeatmapParser.HitObjects.HitSounds;
using BeatmapParser.TimingPoints;
using MapWizard.Tools.HelperExtensions;
using MapWizard.Tools.MapCleaner.Analysis;
using MapWizard.Tools.MapCleaner.Snapping;
using MapWizard.Tools.MapCleaner.Timing;

namespace MapWizard.Tools.MapCleaner;

public static class MapCleaner
{
    public static MapCleanerAnalysis AnalyzeBeatmap(Beatmap beatmap)
    {
        return MapCleanerAnalyzer.Analyze(beatmap);
    }

    public static MapCleanerResult CleanBeatmap(Beatmap beatmap, MapCleanerOptions? options = null)
    {
        options ??= new MapCleanerOptions();

        var result = new MapCleanerResult();
        var divisors = StableSnapEngine.ParseDivisors(options.SnapDivisors);

        var analysis = options.AnalyzeSamples ? MapCleanerAnalyzer.Analyze(beatmap) : new MapCleanerAnalysis();
        result.Analysis = analysis;

        if (options.ResnapGreenLines)
        {
            result.GreenLinesResnapped += ResnapGreenLines(beatmap, divisors, options.RedlineLookaheadForObjectsMs);
        }

        if (options.ResnapObjects)
        {
            foreach (var hitObject in beatmap.HitObjects.Objects)
            {
                var originalStart = hitObject.Time.TotalMilliseconds;
                var snappedStart = StableSnapEngine.SnapMilliseconds(
                    originalStart,
                    beatmap.TimingPoints,
                    divisors,
                    options.RedlineLookaheadForObjectsMs);

                if (Math.Abs(snappedStart - originalStart) > 0.0001)
                {
                    hitObject.Time = TimeSpan.FromMilliseconds(snappedStart);
                    result.ObjectsResnapped++;
                }

                switch (hitObject)
                {
                    case Slider slider:
                    {
                        var originalEnd = slider.EndTime.TotalMilliseconds;
                        var originalDurationMs = Math.Max(1.0, originalEnd - originalStart);
                        var originalLength = slider.Length;
                        var slideCount = Math.Max(1, (int)slider.Slides);
                        var originalSlideDurationMs = Math.Max(1.0, originalDurationMs / slideCount);

                        if (options.ResnapSliderEnds)
                        {
                            var baselineFirstSlideEndMs = slider.Time.TotalMilliseconds + originalSlideDurationMs;
                            var currentFirstSlideEndMs = originalStart + originalSlideDurationMs;
                            var snappedFromBaseline = StableSnapEngine.SnapMilliseconds(
                                baselineFirstSlideEndMs,
                                beatmap.TimingPoints,
                                divisors,
                                options.RedlineLookaheadForEndsMs);
                            var snappedFromCurrent = StableSnapEngine.SnapMilliseconds(
                                currentFirstSlideEndMs,
                                beatmap.TimingPoints,
                                divisors,
                                options.RedlineLookaheadForEndsMs);

                            var snappedFirstSlideEnd = Math.Abs(snappedFromBaseline - baselineFirstSlideEndMs) <= Math.Abs(snappedFromCurrent - baselineFirstSlideEndMs)
                                ? snappedFromBaseline
                                : snappedFromCurrent;

                            var minimumFirstSlideEnd = slider.Time.TotalMilliseconds + 1;
                            if (snappedFirstSlideEnd < minimumFirstSlideEnd)
                            {
                                snappedFirstSlideEnd = (int)minimumFirstSlideEnd;
                            }

                            var snappedSlideDurationMs = Math.Max(1.0, snappedFirstSlideEnd - slider.Time.TotalMilliseconds);
                            var snappedEnd = StableSnapEngine.StableRound(slider.Time.TotalMilliseconds + (snappedSlideDurationMs * slideCount));

                            var minimumEnd = slider.Time.TotalMilliseconds + 1;
                            if (snappedEnd < minimumEnd)
                            {
                                snappedEnd = StableSnapEngine.StableRound(minimumEnd);
                            }

                            if (Math.Abs(snappedEnd - originalEnd) > 0.0001)
                            {
                                ApplySliderEndResnap(
                                    beatmap,
                                    slider,
                                    snappedEnd,
                                    originalLength,
                                    originalDurationMs);
                                result.SliderEndsResnapped++;
                            }
                        }

                        break;
                    }
                    case Spinner spinner:
                    {
                        var originalEnd = spinner.End.TotalMilliseconds;
                        var snappedEnd = StableSnapEngine.SnapMilliseconds(
                            originalEnd,
                            beatmap.TimingPoints,
                            divisors,
                            options.RedlineLookaheadForEndsMs);

                        var minimumEnd = spinner.Time.TotalMilliseconds + 1;
                        if (snappedEnd < minimumEnd)
                        {
                            snappedEnd = (int)minimumEnd;
                        }

                        if (Math.Abs(snappedEnd - originalEnd) > 0.0001)
                        {
                            spinner.End = TimeSpan.FromMilliseconds(snappedEnd);
                            result.SpinnerOrHoldEndsResnapped++;
                        }

                        break;
                    }
                    case ManiaHold maniaHold:
                    {
                        var originalEnd = maniaHold.End.TotalMilliseconds;
                        var snappedEnd = StableSnapEngine.SnapMilliseconds(
                            originalEnd,
                            beatmap.TimingPoints,
                            divisors,
                            options.RedlineLookaheadForEndsMs);

                        var minimumEnd = maniaHold.Time.TotalMilliseconds + 1;
                        if (snappedEnd < minimumEnd)
                        {
                            snappedEnd = (int)minimumEnd;
                        }

                        if (Math.Abs(snappedEnd - originalEnd) > 0.0001)
                        {
                            maniaHold.End = TimeSpan.FromMilliseconds(snappedEnd);
                            result.SpinnerOrHoldEndsResnapped++;
                        }

                        break;
                    }
                }
            }
        }

        if (options.ResnapBookmarks && beatmap.Editor?.Bookmarks != null)
        {
            var bookmarks = beatmap.Editor.Bookmarks;
            for (var i = 0; i < bookmarks.Count; i++)
            {
                var originalBookmark = bookmarks[i].TotalMilliseconds;
                var snappedBookmark = StableSnapEngine.SnapMilliseconds(
                    originalBookmark,
                    beatmap.TimingPoints,
                    divisors,
                    options.RedlineLookaheadForObjectsMs);

                if (snappedBookmark == originalBookmark)
                {
                    continue;
                }

                bookmarks[i] = TimeSpan.FromMilliseconds(snappedBookmark);
                result.BookmarksResnapped++;
            }

            beatmap.Editor.Bookmarks = bookmarks;
        }

        if (options.RemoveHitSounds)
        {
            result.HitSoundsRemoved += RemoveHitSounds(beatmap);
        }

        if (options.RemoveMuting)
        {
            result.MutedTimingPointsRestored += RemoveMuting(beatmap);
        }

        if (options.MuteUnclickableHitsounds)
        {
            result.UnclickableHitSoundsMuted += MuteUnclickableHitSounds(beatmap);
        }

        if (options.RemoveUnusedInheritedTimingPoints)
        {
            // Run the analyzer if analysis was skipped but pruning is requested.
            if (!options.AnalyzeSamples)
            {
                analysis = MapCleanerAnalyzer.Analyze(beatmap);
                result.Analysis = analysis;
            }

            result.InheritedTimingPointsRemoved += InheritedPruner.PruneUnusedInheritedTimingPoints(beatmap, analysis);
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

                BackupOriginalBeatmap(targetPath);

                File.WriteAllText(targetPath, beatmap.Encode().Replace("\r\n", "\n").Replace("\n", "\r\n"));

                batchResult.ProcessedBeatmaps++;
                batchResult.Add(result);
            }
            catch (Exception ex)
            {
                batchResult.FailedBeatmaps++;
                batchResult.FailedPaths.Add(targetPath);
                batchResult.FailureDetails.Add($"'{targetPath}': {ex.Message}");
            }
        }

        return batchResult;
    }

    private static int ResnapGreenLines(Beatmap beatmap, IReadOnlyList<SnapDivisor> divisors, int lookaheadMs)
    {
        if (beatmap.TimingPoints == null || beatmap.TimingPoints.TimingPointList.Count == 0)
        {
            return 0;
        }

        var resnapped = 0;
        foreach (var greenLine in beatmap.TimingPoints.TimingPointList.OfType<InheritedTimingPoint>())
        {
            var originalTime = greenLine.Time.TotalMilliseconds;
            var snappedTime = StableSnapEngine.SnapMilliseconds(
                originalTime,
                beatmap.TimingPoints,
                divisors,
                lookaheadMs);

            if (Math.Abs(snappedTime - originalTime) <= 0.0001)
            {
                continue;
            }

            greenLine.Time = TimeSpan.FromMilliseconds(snappedTime);
            resnapped++;
        }

        if (resnapped > 0)
        {
            SortTimingPoints(beatmap);
        }

        return resnapped;
    }

    private static void BackupOriginalBeatmap(string targetPath)
    {
        var currentTimestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        var backupFileName = $"{currentTimestamp}-{Path.GetFileName(targetPath)}";
        var exceptions = new List<Exception>();

        try
        {
            var backupDirectory = Directory.CreateDirectory(MapWizardPathResolver.ResolveBackupDirectoryPath());
            var backupPath = Path.Combine(backupDirectory.FullName, backupFileName);
            File.Move(targetPath, backupPath, overwrite: true);
            return;
        }
        catch (Exception ex)
        {
            exceptions.Add(ex);
        }

        try
        {
            var fallbackDirectory = Directory.CreateDirectory(Path.Combine(
                Path.GetDirectoryName(targetPath) ?? ".",
                ".mapwizard-backup"));
            var fallbackPath = Path.Combine(fallbackDirectory.FullName, backupFileName);
            File.Move(targetPath, fallbackPath, overwrite: true);
            return;
        }
        catch (Exception ex)
        {
            exceptions.Add(ex);
        }

        throw new AggregateException("Failed to create backup for beatmap file.", exceptions);
    }

    private static void ApplySliderEndResnap(
        Beatmap beatmap,
        Slider slider,
        int snappedEndMs,
        double originalLength,
        double originalDurationMs)
    {
        var startMs = slider.Time.TotalMilliseconds;
        var duration = Math.Max(1, snappedEndMs - startMs);
        var slides = Math.Max(1, (int)slider.Slides);

        var sliderMultiplier = Math.Abs(beatmap.DifficultySection.SliderMultiplier);
        if (sliderMultiplier <= 0.00001)
        {
            sliderMultiplier = 1.0;
        }

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

        var recalculatedLength = (duration * sliderMultiplier * 100.0 * sliderVelocity) / (beatLength * slides);
        var fallbackLength = originalLength * (duration / Math.Max(1.0, originalDurationMs));

        if (double.IsFinite(fallbackLength) && fallbackLength > 0.00001)
        {
            var upperGuard = Math.Max(fallbackLength * 4.0, originalLength * 4.0);
            var lowerGuard = Math.Min(fallbackLength * 0.25, Math.Max(0.00001, originalLength * 0.25));

            if (!double.IsFinite(recalculatedLength) || recalculatedLength > upperGuard || recalculatedLength < lowerGuard)
            {
                recalculatedLength = fallbackLength;
            }
        }

        if (double.IsFinite(recalculatedLength) && recalculatedLength > 0.00001)
        {
            slider.Length = recalculatedLength;
        }

        slider.EndTime = TimeSpan.FromMilliseconds(snappedEndMs);
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

    private static int RemoveHitSounds(Beatmap beatmap)
    {
        var removed = 0;

        foreach (var hitObject in beatmap.HitObjects.Objects)
        {
            switch (hitObject)
            {
                case Circle circle:
                {
                    if (!HasAnyHitSoundData(circle.HitSounds))
                    {
                        break;
                    }

                    circle.HitSounds = (new HitSample(), []);
                    removed++;
                    break;
                }
                case Spinner spinner:
                {
                    if (!HasAnyHitSoundData(spinner.HitSounds))
                    {
                        break;
                    }

                    spinner.HitSounds = (new HitSample(), []);
                    removed++;
                    break;
                }
                case ManiaHold maniaHold:
                {
                    if (!HasAnyHitSoundData(maniaHold.HitSounds))
                    {
                        break;
                    }

                    maniaHold.HitSounds = (new HitSample(), []);
                    removed++;
                    break;
                }
                case Slider slider:
                {
                    var hadHitSounds = HasAnyHitSoundData(slider.HitSounds) ||
                                       HasAnyHitSoundData(slider.HeadSounds) ||
                                       HasAnyHitSoundData(slider.TailSounds) ||
                                       (slider.RepeatSounds != null && slider.RepeatSounds.Any(HasAnyHitSoundData));

                    if (!hadHitSounds)
                    {
                        break;
                    }

                    slider.HitSounds = (new HitSample(), []);
                    slider.HeadSounds = (new HitSample(), []);
                    slider.TailSounds = (new HitSample(), []);

                    if (slider.RepeatSounds != null)
                    {
                        for (var i = 0; i < slider.RepeatSounds.Count; i++)
                        {
                            slider.RepeatSounds[i] = (new HitSample(), []);
                        }
                    }

                    removed++;
                    break;
                }
            }
        }

        return removed;
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

    private static int MuteUnclickableHitSounds(Beatmap beatmap)
    {
        var muted = 0;

        foreach (var slider in beatmap.HitObjects.Objects.OfType<Slider>())
        {
            if (!HasAnyHitSoundData(slider.HitSounds))
            {
                continue;
            }

            slider.HitSounds = (new HitSample(), []);
            muted++;
        }

        return muted;
    }

    private static bool HasAnyHitSoundData((HitSample SampleData, List<BeatmapParser.Enums.HitSound> HitSounds) hitSoundSet)
    {
        var (sampleData, hitSounds) = hitSoundSet;
        return hitSounds.Count > 0 || !string.IsNullOrWhiteSpace(sampleData.FileName);
    }
}
