using BeatmapParser;
using BeatmapParser.HitObjects;
using BeatmapParser.TimingPoints;
using MapWizard.Tools.HitSounds.Copier;
using MapWizard.Tools.MapCleaner;
using MapWizard.Tools.MapCleaner.Snapping;

namespace MapWizard.Tests.MapCleaner;

public class MapCleanerTests
{
    [Fact]
    public void CleanBeatmap_WithAllOptionsDisabled_DoesNotMutateBeatmap()
    {
        var beatmap = Beatmap.Decode(GetResnapEverythingBeatmap());
        var encodedBefore = beatmap.Encode();

        var result = MapWizard.Tools.MapCleaner.MapCleaner.CleanBeatmap(beatmap, new MapCleanerOptions
        {
            ResnapEverything = false,
            RemoveMuting = false,
            RemoveUnusedGreenlines = false
        });

        Assert.Equal(encodedBefore, beatmap.Encode());
        Assert.Equal(0, result.TimingPointsResnapped);
        Assert.Equal(0, result.ObjectsResnapped);
        Assert.Equal(0, result.SliderEndsResnapped);
        Assert.Equal(0, result.SpinnerOrHoldEndsResnapped);
        Assert.Equal(0, result.BookmarksResnapped);
        Assert.Equal(0, result.PreviewTimeResnapped);
        Assert.Equal(0, result.GreenLinesResnapped);
        Assert.Equal(0, result.GreenLinesRemoved);
        Assert.Equal(0, result.MutedTimingPointsRestored);
    }

    [Fact]
    public void CleanBeatmap_ResnapEverything_ResnapsTimingPointsObjectsBookmarksAndPreview()
    {
        var beatmap = Beatmap.Decode(GetResnapEverythingBeatmap());
        var greenline = beatmap.TimingPoints!.TimingPointList.OfType<InheritedTimingPoint>().Single();
        var secondRedline = beatmap.TimingPoints.TimingPointList.OfType<UninheritedTimingPoint>().Last();
        var circle = beatmap.HitObjects.Objects.OfType<Circle>().Single();

        var result = MapWizard.Tools.MapCleaner.MapCleaner.CleanBeatmap(beatmap, new MapCleanerOptions
        {
            ResnapEverything = true,
            RemoveMuting = false,
            RemoveUnusedGreenlines = false,
            SnapDivisors = ["1/4"]
        });

        Assert.Equal(TimeSpan.FromMilliseconds(125), circle.Time);
        Assert.Equal(TimeSpan.FromMilliseconds(125), greenline.Time);
        Assert.Equal(TimeSpan.FromMilliseconds(626), secondRedline.Time);
        Assert.NotNull(beatmap.Editor);
        var editor = beatmap.Editor!;
        Assert.NotNull(editor.Bookmarks);
        Assert.Equal(TimeSpan.FromMilliseconds(125), editor.Bookmarks[0]);
        Assert.Equal(125, beatmap.GeneralSection.PreviewTime);
        Assert.Equal(2, result.TimingPointsResnapped);
        Assert.Equal(1, result.GreenLinesResnapped);
        Assert.Equal(1, result.ObjectsResnapped);
        Assert.Equal(1, result.BookmarksResnapped);
        Assert.Equal(1, result.PreviewTimeResnapped);
    }

    [Fact]
    public void CleanBeatmap_ResnapEverything_ResnapsSpinnerEnd()
    {
        var beatmap = Beatmap.Decode(GetSpinnerBeatmap());
        var spinner = beatmap.HitObjects.Objects.OfType<Spinner>().Single();

        var result = MapWizard.Tools.MapCleaner.MapCleaner.CleanBeatmap(beatmap, new MapCleanerOptions
        {
            ResnapEverything = true,
            RemoveMuting = false,
            RemoveUnusedGreenlines = false,
            SnapDivisors = ["1/4"]
        });

        Assert.Equal(TimeSpan.FromMilliseconds(0), spinner.Time);
        Assert.Equal(TimeSpan.FromMilliseconds(125), spinner.End);
        Assert.Equal(1, result.ObjectsResnapped);
        Assert.Equal(1, result.SpinnerOrHoldEndsResnapped);
    }

    [Fact]
    public void CleanBeatmap_ResnapEverything_ResnapsHoldEnd()
    {
        var beatmap = Beatmap.Decode(GetManiaHoldBeatmap());
        var hold = beatmap.HitObjects.Objects.OfType<ManiaHold>().Single();

        var result = MapWizard.Tools.MapCleaner.MapCleaner.CleanBeatmap(beatmap, new MapCleanerOptions
        {
            ResnapEverything = true,
            RemoveMuting = false,
            RemoveUnusedGreenlines = false,
            SnapDivisors = ["1/4"]
        });

        Assert.Equal(TimeSpan.FromMilliseconds(0), hold.Time);
        Assert.Equal(TimeSpan.FromMilliseconds(125), hold.End);
        Assert.Equal(1, result.ObjectsResnapped);
        Assert.Equal(1, result.SpinnerOrHoldEndsResnapped);
    }

    [Fact]
    public void CleanBeatmap_ResnapEverything_ResnapsSliderByFullDuration()
    {
        var beatmap = Beatmap.Decode(GetTripleRepeatSliderBeatmap());
        var slider = beatmap.HitObjects.Objects.OfType<Slider>().Single();

        var result = MapWizard.Tools.MapCleaner.MapCleaner.CleanBeatmap(beatmap, new MapCleanerOptions
        {
            ResnapEverything = true,
            RemoveMuting = false,
            RemoveUnusedGreenlines = false,
            SnapDivisors = ["1/4"]
        });

        Assert.Equal(TimeSpan.FromMilliseconds(0), slider.Time);
        Assert.Equal(TimeSpan.FromMilliseconds(250), slider.EndTime);
        Assert.InRange(slider.Length, 23.332, 23.334);
        Assert.Equal(1, result.ObjectsResnapped);
        Assert.Equal(1, result.SliderEndsResnapped);
    }

    [Fact]
    public void CleanBeatmap_ResnapEverything_ResnapsRedlinesRelativeToPreviousReferenceRedline()
    {
        var beatmap = Beatmap.Decode(GetRelativeRedlineBeatmap());

        var result = MapWizard.Tools.MapCleaner.MapCleaner.CleanBeatmap(beatmap, new MapCleanerOptions
        {
            ResnapEverything = true,
            RemoveMuting = false,
            RemoveUnusedGreenlines = false,
            SnapDivisors = ["1/4"]
        });

        var redlines = beatmap.TimingPoints!.TimingPointList
            .OfType<UninheritedTimingPoint>()
            .OrderBy(x => x.Time.TotalMilliseconds)
            .ToList();

        Assert.Equal(TimeSpan.FromMilliseconds(0), redlines[0].Time);
        Assert.Equal(TimeSpan.FromMilliseconds(378), redlines[1].Time);
        Assert.Equal(TimeSpan.FromMilliseconds(878), redlines[2].Time);
        Assert.Equal(2, result.TimingPointsResnapped);
    }

    [Fact]
    public void CleanBeatmap_ResnapEverything_DoesNotMoveRedlineAnchoredToObjectStart()
    {
        var beatmap = Beatmap.Decode(GetAnchoredRedlineBeatmap());

        var result = MapWizard.Tools.MapCleaner.MapCleaner.CleanBeatmap(beatmap, new MapCleanerOptions
        {
            ResnapEverything = true,
            RemoveMuting = false,
            RemoveUnusedGreenlines = false,
            SnapDivisors = ["1/4"]
        });

        var redlines = beatmap.TimingPoints!.TimingPointList
            .OfType<UninheritedTimingPoint>()
            .OrderBy(x => x.Time.TotalMilliseconds)
            .ToList();
        var slider = beatmap.HitObjects.Objects.OfType<Slider>().Single();

        Assert.Equal(TimeSpan.FromMilliseconds(3012), redlines[1].Time);
        Assert.Equal(TimeSpan.FromMilliseconds(3012), slider.Time);
        Assert.Equal(0, result.TimingPointsResnapped);
        Assert.Equal(0, result.ObjectsResnapped);
    }

    [Fact]
    public void CleanBeatmap_ResnapEverything_DoesNotMoveGreenlineAnchoredToSliderStart()
    {
        var beatmap = Beatmap.Decode(GetAnchoredGreenlineBeatmap());

        var result = MapWizard.Tools.MapCleaner.MapCleaner.CleanBeatmap(beatmap, new MapCleanerOptions
        {
            ResnapEverything = true,
            RemoveMuting = false,
            RemoveUnusedGreenlines = false,
            SnapDivisors = ["1/4"]
        });

        var greenline = beatmap.TimingPoints!.TimingPointList.OfType<InheritedTimingPoint>().Single();
        var redline = beatmap.TimingPoints.TimingPointList.OfType<UninheritedTimingPoint>().Last();
        var slider = beatmap.HitObjects.Objects.OfType<Slider>().Single();

        Assert.Equal(TimeSpan.FromMilliseconds(3012), greenline.Time);
        Assert.Equal(TimeSpan.FromMilliseconds(3012), redline.Time);
        Assert.Equal(TimeSpan.FromMilliseconds(3012), slider.Time);
        Assert.Equal(0, result.TimingPointsResnapped);
        Assert.Equal(0, result.GreenLinesResnapped);
        Assert.Equal(1, result.SliderEndsResnapped);
    }

    [Fact]
    public void CleanBeatmap_ResnapEverything_ResnapsOffSnapSliderEndEvenWhenStartDoesNotMove()
    {
        var beatmap = Beatmap.Decode(GetAnchoredGreenlineOffSnapSliderEndBeatmap());
        var slider = beatmap.HitObjects.Objects.OfType<Slider>().Single();

        var result = MapWizard.Tools.MapCleaner.MapCleaner.CleanBeatmap(beatmap, new MapCleanerOptions
        {
            ResnapEverything = true,
            RemoveMuting = false,
            RemoveUnusedGreenlines = false,
            SnapDivisors = ["1/4"]
        });

        Assert.Equal(TimeSpan.FromMilliseconds(3012), slider.Time);
        Assert.InRange(slider.EndTime.TotalMilliseconds, 3345.3332, 3345.3334);
        Assert.Equal(1, result.SliderEndsResnapped);
    }

    [Fact]
    public void CleanBeatmap_ResnapEverything_ResnapsGreenlineWithoutAffectedObjects()
    {
        var beatmap = Beatmap.Decode(GetUnanchoredGreenlineBeatmap());
        var greenline = beatmap.TimingPoints!.TimingPointList.OfType<InheritedTimingPoint>().Single();
        var originalTime = greenline.Time.TotalMilliseconds;
        var expectedTime = StableSnapEngine.SnapMilliseconds(
            originalTime,
            beatmap.TimingPoints,
            StableSnapEngine.ParseDivisors(["1/4"]),
            forwardRedlineWindowMs: 10);

        Assert.NotEqual(originalTime, expectedTime);

        var result = MapWizard.Tools.MapCleaner.MapCleaner.CleanBeatmap(beatmap, new MapCleanerOptions
        {
            ResnapEverything = true,
            RemoveMuting = false,
            RemoveUnusedGreenlines = false,
            SnapDivisors = ["1/4"]
        });

        Assert.Equal(TimeSpan.FromMilliseconds(expectedTime), greenline.Time);
        Assert.True(result.GreenLinesResnapped >= 1);
    }

    [Fact]
    public void CleanBeatmap_ResnapEverything_ResnapsGreenlineToClosestRedlinePhase()
    {
        var beatmap = Beatmap.Decode(GetClosestRedlinePhaseGreenlineBeatmap());
        var greenline = beatmap.TimingPoints!.TimingPointList.OfType<InheritedTimingPoint>().Single();

        var result = MapWizard.Tools.MapCleaner.MapCleaner.CleanBeatmap(beatmap, new MapCleanerOptions
        {
            ResnapEverything = true,
            RemoveMuting = false,
            RemoveUnusedGreenlines = false,
            SnapDivisors = ["1/8", "1/12"]
        });

        Assert.Equal(TimeSpan.FromMilliseconds(52429), greenline.Time);
        Assert.True(result.GreenLinesResnapped >= 1);
    }

    [Fact]
    public void CleanBeatmap_ResnapEverything_ResnapsSliderEndAgainstCleanedRedlinePhase()
    {
        var beatmap = Beatmap.Decode(GetSliderTailOffSnapAfterRedlineResnapBeatmap());
        var slider = beatmap.HitObjects.Objects.OfType<Slider>().Single();

        var result = MapWizard.Tools.MapCleaner.MapCleaner.CleanBeatmap(beatmap, new MapCleanerOptions
        {
            ResnapEverything = true,
            RemoveMuting = false,
            RemoveUnusedGreenlines = false,
            SnapDivisors = ["1/4"]
        });

        var redline = beatmap.TimingPoints!.TimingPointList.OfType<UninheritedTimingPoint>().Single();

        Assert.Equal(TimeSpan.FromMilliseconds(0), redline.Time);
        Assert.Equal(TimeSpan.FromMilliseconds(0), slider.Time);
        Assert.InRange(slider.EndTime.TotalMilliseconds, 83.3332, 83.3334);
        Assert.InRange(slider.Length, 34.999, 35.001);
        Assert.Equal(1, result.TimingPointsResnapped);
        Assert.Equal(0, result.ObjectsResnapped);
        Assert.Equal(1, result.SliderEndsResnapped);
    }

    [Fact]
    public void CleanBeatmap_ResnapEverything_ReverseSliderWithMultipleAffectedSpans_PrioritizesAllEdgesUnderTwoMilliseconds()
    {
        var beatmap = Beatmap.Decode(GetReverseSliderMultipleSpanRedlineBeatmap());
        var slider = beatmap.HitObjects.Objects.OfType<Slider>().Single();
        var divisors = StableSnapEngine.ParseDivisors(["1/4"]);

        var result = MapWizard.Tools.MapCleaner.MapCleaner.CleanBeatmap(beatmap, new MapCleanerOptions
        {
            ResnapEverything = true,
            RemoveMuting = false,
            RemoveUnusedGreenlines = false,
            SnapDivisors = ["1/4"]
        });

        Assert.Equal(TimeSpan.FromMilliseconds(0), slider.Time);
        Assert.Equal(TimeSpan.FromMilliseconds(450), slider.EndTime);
        Assert.InRange(slider.Length, 41.999, 42.001);
        Assert.Equal(0, result.ObjectsResnapped);
        Assert.Equal(1, result.SliderEndsResnapped);

        var duration = slider.EndTime.TotalMilliseconds - slider.Time.TotalMilliseconds;
        var firstEdgeTime = slider.Time.TotalMilliseconds + (duration / slider.Slides);
        var snappedFirstEdge = StableSnapEngine.SnapMilliseconds(firstEdgeTime, beatmap.TimingPoints, divisors, 10);
        Assert.Equal(30, Math.Abs(snappedFirstEdge - firstEdgeTime), precision: 3);
    }

    [Fact]
    public void CleanBeatmap_ResnapEverything_ReverseSliderWithSnappedTailButOffSnapRepeat_KeepsEstimatedSpanDuration()
    {
        var beatmap = Beatmap.Decode(GetReverseSliderFromRecallTheEndBeatmap());
        var slider = beatmap.HitObjects.Objects.OfType<Slider>().Single();
        var divisors = StableSnapEngine.ParseDivisors(["1/8", "1/12"]);

        var result = MapWizard.Tools.MapCleaner.MapCleaner.CleanBeatmap(beatmap, new MapCleanerOptions
        {
            ResnapEverything = true,
            RemoveMuting = false,
            RemoveUnusedGreenlines = false
        });

        Assert.Equal(TimeSpan.FromMilliseconds(218716), slider.Time);
        Assert.Equal(TimeSpan.FromMilliseconds(218996), slider.EndTime);
        Assert.InRange(slider.Length, 41.215, 41.217);
        Assert.Equal(1, result.ObjectsResnapped);
        Assert.Equal(1, result.SliderEndsResnapped);

        var duration = slider.EndTime.TotalMilliseconds - slider.Time.TotalMilliseconds;
        var secondRepeatTime = slider.Time.TotalMilliseconds + (duration * 2 / slider.Slides);
        var snappedSecondRepeat = StableSnapEngine.SnapMilliseconds(secondRepeatTime, beatmap.TimingPoints, divisors, 10);

        Assert.Equal(2.6666666666569654, Math.Abs(snappedSecondRepeat - secondRepeatTime), precision: 6);
    }

    [Fact]
    public void CleanBeatmap_RemoveMuting_RestoresMutedTimingPoints()
    {
        var beatmap = Beatmap.Decode(GetMutedTimingPointBeatmap());

        var result = MapWizard.Tools.MapCleaner.MapCleaner.CleanBeatmap(beatmap, new MapCleanerOptions
        {
            ResnapEverything = false,
            RemoveMuting = true,
            RemoveUnusedGreenlines = false
        });

        var timingPoints = beatmap.TimingPoints!.TimingPointList.OrderBy(x => x.Time.TotalMilliseconds).ToList();
        Assert.Equal((uint)70, timingPoints[0].Volume);
        Assert.Equal((uint)70, timingPoints[1].Volume);
        Assert.Equal((uint)70, timingPoints[2].Volume);
        Assert.Equal((uint)80, timingPoints[3].Volume);
        Assert.Equal(2, result.MutedTimingPointsRestored);
    }

    [Fact]
    public void CleanBeatmap_RemoveUnusedGreenlines_RemovesUnusedAndRedundantGreenlines()
    {
        var beatmap = Beatmap.Decode(GetGreenlinePruningBeatmap());

        var result = MapWizard.Tools.MapCleaner.MapCleaner.CleanBeatmap(beatmap, new MapCleanerOptions
        {
            ResnapEverything = false,
            RemoveMuting = false,
            RemoveUnusedGreenlines = true
        });

        var inheritedTimingPoints = beatmap.TimingPoints!.TimingPointList.OfType<InheritedTimingPoint>().ToList();
        Assert.Single(inheritedTimingPoints);
        Assert.Equal(TimeSpan.FromMilliseconds(650), inheritedTimingPoints[0].Time);
        Assert.Equal(2, result.GreenLinesRemoved);
    }

    [Fact]
    public void CleanBeatmap_RemoveUnusedGreenlines_KeepsGreenlineThatAffectsSliderBody()
    {
        var beatmap = Beatmap.Decode(GetSliderBodyGreenlineBeatmap());

        var result = MapWizard.Tools.MapCleaner.MapCleaner.CleanBeatmap(beatmap, new MapCleanerOptions
        {
            ResnapEverything = false,
            RemoveMuting = false,
            RemoveUnusedGreenlines = true
        });

        var inheritedTimingPoints = beatmap.TimingPoints!.TimingPointList
            .OfType<InheritedTimingPoint>()
            .OrderBy(x => x.Time.TotalMilliseconds)
            .ToList();

        Assert.Single(inheritedTimingPoints);
        Assert.Equal(TimeSpan.FromMilliseconds(450), inheritedTimingPoints[0].Time);
        Assert.Equal(2.0, inheritedTimingPoints[0].SliderVelocity, precision: 3);
        Assert.Equal(2, result.GreenLinesRemoved);
    }

    [Fact]
    public void CleanBeatmap_RemoveUnusedGreenlines_ResnapsVolumeGreenlineToClosestSliderTick()
    {
        var beatmap = Beatmap.Decode(GetVolumeTickAnchoringBeatmap());

        var result = MapWizard.Tools.MapCleaner.MapCleaner.CleanBeatmap(beatmap, new MapCleanerOptions
        {
            ResnapEverything = false,
            RemoveMuting = false,
            RemoveUnusedGreenlines = true
        });

        var greenline = beatmap.TimingPoints!.TimingPointList.OfType<InheritedTimingPoint>().Single();
        Assert.Equal(TimeSpan.FromMilliseconds(500), greenline.Time);
        Assert.Equal((uint)40, greenline.Volume);
        Assert.Equal(1, result.GreenLinesResnapped);
        Assert.Equal(0, result.GreenLinesRemoved);
    }

    [Fact]
    public void CleanBeatmap_RemoveUnusedGreenlines_DoesNotUseSliderTickAsAnchorWhenVolumeUnchanged()
    {
        var beatmap = Beatmap.Decode(GetSampleSetOnlyGreenlineBeatmap());

        var result = MapWizard.Tools.MapCleaner.MapCleaner.CleanBeatmap(beatmap, new MapCleanerOptions
        {
            ResnapEverything = false,
            RemoveMuting = false,
            RemoveUnusedGreenlines = true
        });

        var greenline = beatmap.TimingPoints!.TimingPointList.OfType<InheritedTimingPoint>().Single();
        Assert.Equal(TimeSpan.FromMilliseconds(1500), greenline.Time);
        Assert.Equal(BeatmapParser.Enums.SampleSet.Soft, greenline.SampleSet);
        Assert.Equal(1, result.GreenLinesResnapped);
        Assert.Equal(0, result.GreenLinesRemoved);
    }

    [Fact]
    public void CleanBeatmapTargets_MissingFile_ReportsFailure()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), "mapwizard-mapcleaner-missing-" + Guid.NewGuid().ToString("N") + ".osu");

        var result = MapWizard.Tools.MapCleaner.MapCleaner.CleanBeatmapTargets([missingPath], new MapCleanerOptions());

        Assert.Equal(0, result.ProcessedBeatmaps);
        Assert.Equal(1, result.FailedBeatmaps);
        Assert.Contains(missingPath, result.FailedPaths);
        Assert.Single(result.FailureDetails);
    }

    [Fact]
    public void CleanBeatmapTargets_ValidFile_WritesCleanedBeatmapCreatesBackupAndAggregatesCounts()
    {
        if (OperatingSystem.IsMacOS())
        {
            return;
        }

        var sandboxRoot = CreateSandbox("mapwizard-mapcleaner-targets");
        var previousXdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        Environment.SetEnvironmentVariable("XDG_DATA_HOME", sandboxRoot);

        try
        {
            var beatmapPath = Path.Combine(sandboxRoot, "target.osu");
            File.WriteAllText(beatmapPath, GetResnapEverythingBeatmap());

            var result = MapWizard.Tools.MapCleaner.MapCleaner.CleanBeatmapTargets([beatmapPath], new MapCleanerOptions
            {
                ResnapEverything = true,
                RemoveMuting = false,
                RemoveUnusedGreenlines = false,
                SnapDivisors = ["1/4"]
            });

            var cleanedBeatmap = Beatmap.Decode(File.ReadAllText(beatmapPath));
            var cleanedCircle = cleanedBeatmap.HitObjects.Objects.OfType<Circle>().Single();
            var backupDirectory = Path.Combine(sandboxRoot, "MapWizard", "Backup");

            Assert.Equal(1, result.ProcessedBeatmaps);
            Assert.Equal(0, result.FailedBeatmaps);
            Assert.Equal(2, result.TimingPointsResnapped);
            Assert.Equal(1, result.ObjectsResnapped);
            Assert.Equal(1, result.BookmarksResnapped);
            Assert.Equal(1, result.PreviewTimeResnapped);
            Assert.Equal(1, result.GreenLinesResnapped);
            Assert.Equal(TimeSpan.FromMilliseconds(125), cleanedCircle.Time);
            Assert.True(Directory.Exists(backupDirectory));
            Assert.NotEmpty(Directory.GetFiles(backupDirectory, "*.osu"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("XDG_DATA_HOME", previousXdgDataHome);
            Directory.Delete(sandboxRoot, recursive: true);
        }
    }

    [Fact]
    public void HitSoundCopierThenMapCleaner_DoesNotTurnSilentSliderTailIntoHeadHitsound()
    {
        var source = Beatmap.Decode(GetHeadOnlySliderSourceBeatmap());
        var target = Beatmap.Decode(GetHeadOnlySliderTargetBeatmap());

        var copied = global::MapWizard.Tools.HitSounds.Copier.HitSoundCopier.CopyFromBeatmap(source, target, new HitSoundCopierOptions
        {
            CopySliderBodySounds = false,
            OverwriteEverything = true
        });

        var copiedSlider = copied.HitObjects.Objects.OfType<Slider>().Single();
        Assert.Single(copiedSlider.HeadSounds.Sounds);
        Assert.Contains(BeatmapParser.Enums.HitSound.Whistle, copiedSlider.HeadSounds.Sounds);
        Assert.Empty(copiedSlider.TailSounds.Sounds);

        var reloaded = Beatmap.Decode(copied.Encode());
        MapWizard.Tools.MapCleaner.MapCleaner.CleanBeatmap(reloaded, new MapCleanerOptions
        {
            ResnapEverything = false,
            RemoveMuting = false,
            RemoveUnusedGreenlines = false
        });

        var finalSlider = reloaded.HitObjects.Objects.OfType<Slider>().Single();
        Assert.Single(finalSlider.HeadSounds.Sounds);
        Assert.Contains(BeatmapParser.Enums.HitSound.Whistle, finalSlider.HeadSounds.Sounds);
        var finalTailSound = Assert.Single(finalSlider.TailSounds.Sounds);
        Assert.Equal(BeatmapParser.Enums.HitSound.None, finalTailSound);
    }

    private static string GetResnapEverythingBeatmap()
    {
        return """
               osu file format v14

               [General]
               AudioFilename: a.mp3
               AudioLeadIn: 0
               PreviewTime: 126
               Countdown: 0
               SampleSet: Normal
               StackLeniency: 0.7
               Mode: 0
               LetterboxInBreaks: 0
               WidescreenStoryboard: 0

               [Editor]
               DistanceSpacing: 1
               BeatDivisor: 4
               GridSize: 4
               TimelineZoom: 1
               Bookmarks: 126

               [Metadata]
               Title: t
               TitleUnicode: t
               Artist: a
               ArtistUnicode: a
               Creator: c
               Version: test
               Source:
               Tags:
               BeatmapID: 0
               BeatmapSetID: -1

               [Difficulty]
               HPDrainRate: 5
               CircleSize: 4
               OverallDifficulty: 8
               ApproachRate: 9
               SliderMultiplier: 1.4
               SliderTickRate: 1

               [Events]
               //Background and Video events

               [TimingPoints]
               0,500,4,1,0,100,1,0
               101.4,-100,4,1,0,100,0,0
               626.4,500,4,1,0,100,1,0

               [HitObjects]
               256,192,126,1,0,0:0:0:0:
               """.Replace("\n", "\r\n");
    }

    private static string GetTripleRepeatSliderBeatmap()
    {
        return """
               osu file format v14

               [General]
               AudioFilename: a.mp3
               AudioLeadIn: 0
               PreviewTime: -1
               Countdown: 0
               SampleSet: Normal
               StackLeniency: 0.7
               Mode: 0
               LetterboxInBreaks: 0
               WidescreenStoryboard: 0

               [Editor]
               DistanceSpacing: 1
               BeatDivisor: 4
               GridSize: 4
               TimelineZoom: 1

               [Metadata]
               Title: t
               TitleUnicode: t
               Artist: a
               ArtistUnicode: a
               Creator: c
               Version: test
               Source:
               Tags:
               BeatmapID: 0
               BeatmapSetID: -1

               [Difficulty]
               HPDrainRate: 5
               CircleSize: 4
               OverallDifficulty: 8
               ApproachRate: 9
               SliderMultiplier: 1.4
               SliderTickRate: 1

               [Events]
               //Background and Video events

               [TimingPoints]
               0,500,4,1,0,100,1,0

               [HitObjects]
               256,192,3,2,0,B|356:192,3,28
               """.Replace("\n", "\r\n");
    }

    private static string GetSpinnerBeatmap()
    {
        return """
               osu file format v14

               [General]
               AudioFilename: a.mp3
               AudioLeadIn: 0
               PreviewTime: -1
               Countdown: 0
               SampleSet: Normal
               StackLeniency: 0.7
               Mode: 0
               LetterboxInBreaks: 0
               WidescreenStoryboard: 0

               [Editor]
               DistanceSpacing: 1
               BeatDivisor: 4
               GridSize: 4
               TimelineZoom: 1

               [Metadata]
               Title: t
               TitleUnicode: t
               Artist: a
               ArtistUnicode: a
               Creator: c
               Version: test
               Source:
               Tags:
               BeatmapID: 0
               BeatmapSetID: -1

               [Difficulty]
               HPDrainRate: 5
               CircleSize: 4
               OverallDifficulty: 8
               ApproachRate: 9
               SliderMultiplier: 1.4
               SliderTickRate: 1

               [Events]
               //Background and Video events

               [TimingPoints]
               0,500,4,1,0,100,1,0

               [HitObjects]
               256,192,3,8,0,126,0:0:0:0:
               """.Replace("\n", "\r\n");
    }

    private static string GetManiaHoldBeatmap()
    {
        return """
               osu file format v14

               [General]
               AudioFilename: a.mp3
               AudioLeadIn: 0
               PreviewTime: -1
               Countdown: 0
               SampleSet: Normal
               StackLeniency: 0.7
               Mode: 3
               LetterboxInBreaks: 0
               WidescreenStoryboard: 0

               [Editor]
               DistanceSpacing: 1
               BeatDivisor: 4
               GridSize: 4
               TimelineZoom: 1

               [Metadata]
               Title: t
               TitleUnicode: t
               Artist: a
               ArtistUnicode: a
               Creator: c
               Version: test
               Source:
               Tags:
               BeatmapID: 0
               BeatmapSetID: -1

               [Difficulty]
               HPDrainRate: 5
               CircleSize: 4
               OverallDifficulty: 8
               ApproachRate: 9
               SliderMultiplier: 1.4
               SliderTickRate: 1

               [Events]
               //Background and Video events

               [TimingPoints]
               0,500,4,1,0,100,1,0

               [HitObjects]
               64,192,3,128,0,126:0:0:0:0:
               """.Replace("\n", "\r\n");
    }

    private static string GetRelativeRedlineBeatmap()
    {
        return """
               osu file format v14

               [General]
               AudioFilename: a.mp3
               AudioLeadIn: 0
               PreviewTime: -1
               Countdown: 0
               SampleSet: Normal
               StackLeniency: 0.7
               Mode: 0
               LetterboxInBreaks: 0
               WidescreenStoryboard: 0

               [Editor]
               DistanceSpacing: 1
               BeatDivisor: 4
               GridSize: 4
               TimelineZoom: 1

               [Metadata]
               Title: t
               TitleUnicode: t
               Artist: a
               ArtistUnicode: a
               Creator: c
               Version: test
               Source:
               Tags:
               BeatmapID: 0
               BeatmapSetID: -1

               [Difficulty]
               HPDrainRate: 5
               CircleSize: 4
               OverallDifficulty: 8
               ApproachRate: 9
               SliderMultiplier: 1.4
               SliderTickRate: 1

               [Events]
               //Background and Video events

               [TimingPoints]
               0,500,4,1,0,100,1,0
               378.4,400,4,1,0,100,1,0
               876.4,400,4,1,0,100,1,0

               [HitObjects]
               256,192,0,1,0,0:0:0:0:
               """.Replace("\n", "\r\n");
    }

    private static string GetMutedTimingPointBeatmap()
    {
        return """
               osu file format v14

               [General]
               AudioFilename: a.mp3
               AudioLeadIn: 0
               PreviewTime: -1
               Countdown: 0
               SampleSet: Normal
               StackLeniency: 0.7
               Mode: 0
               LetterboxInBreaks: 0
               WidescreenStoryboard: 0

               [Editor]
               DistanceSpacing: 1
               BeatDivisor: 4
               GridSize: 4
               TimelineZoom: 1

               [Metadata]
               Title: t
               TitleUnicode: t
               Artist: a
               ArtistUnicode: a
               Creator: c
               Version: test
               Source:
               Tags:
               BeatmapID: 0
               BeatmapSetID: -1

               [Difficulty]
               HPDrainRate: 5
               CircleSize: 4
               OverallDifficulty: 8
               ApproachRate: 9
               SliderMultiplier: 1.4
               SliderTickRate: 1

               [Events]
               //Background and Video events

               [TimingPoints]
               0,500,4,1,0,70,1,0
               100,-100,4,1,0,5,0,0
               200,-100,4,1,0,4,0,0
               300,500,4,1,0,80,1,0

               [HitObjects]
               256,192,0,1,0,0:0:0:0:
               """.Replace("\n", "\r\n");
    }

    private static string GetAnchoredRedlineBeatmap()
    {
        return """
               osu file format v14

               [General]
               AudioFilename: a.mp3
               AudioLeadIn: 0
               PreviewTime: -1
               Countdown: 0
               SampleSet: Normal
               StackLeniency: 0.7
               Mode: 0
               LetterboxInBreaks: 0
               WidescreenStoryboard: 0

               [Editor]
               DistanceSpacing: 1
               BeatDivisor: 4
               GridSize: 4
               TimelineZoom: 1

               [Metadata]
               Title: t
               TitleUnicode: t
               Artist: a
               ArtistUnicode: a
               Creator: c
               Version: test
               Source:
               Tags:
               BeatmapID: 0
               BeatmapSetID: -1

               [Difficulty]
               HPDrainRate: 5
               CircleSize: 4
               OverallDifficulty: 8
               ApproachRate: 9
               SliderMultiplier: 1.4
               SliderTickRate: 1

               [Events]
               //Background and Video events

               [TimingPoints]
               2354,329.670329670329,4,2,3,40,1,0
               3012,333.333333333333,4,2,3,40,1,0

               [HitObjects]
               256,192,3012,2,0,B|356:192,1,140
               """.Replace("\n", "\r\n");
    }

    private static string GetAnchoredGreenlineBeatmap()
    {
        return """
               osu file format v14

               [General]
               AudioFilename: a.mp3
               AudioLeadIn: 0
               PreviewTime: -1
               Countdown: 0
               SampleSet: Normal
               StackLeniency: 0.7
               Mode: 0
               LetterboxInBreaks: 0
               WidescreenStoryboard: 0

               [Editor]
               DistanceSpacing: 1
               BeatDivisor: 4
               GridSize: 4
               TimelineZoom: 1

               [Metadata]
               Title: t
               TitleUnicode: t
               Artist: a
               ArtistUnicode: a
               Creator: c
               Version: test
               Source:
               Tags:
               BeatmapID: 0
               BeatmapSetID: -1

               [Difficulty]
               HPDrainRate: 5
               CircleSize: 4
               OverallDifficulty: 8
               ApproachRate: 9
               SliderMultiplier: 1.4
               SliderTickRate: 1

               [Events]
               //Background and Video events

               [TimingPoints]
               2354,329.670329670329,4,2,3,40,1,0
               3012,333.333333333333,4,2,3,40,1,0
               3012,-100,4,2,3,40,0,0

               [HitObjects]
               256,192,3012,2,0,B|356:192,1,140
               """.Replace("\n", "\r\n");
    }

    private static string GetAnchoredGreenlineOffSnapSliderEndBeatmap()
    {
        return """
               osu file format v14

               [General]
               AudioFilename: a.mp3
               AudioLeadIn: 0
               PreviewTime: -1
               Countdown: 0
               SampleSet: Normal
               StackLeniency: 0.7
               Mode: 0
               LetterboxInBreaks: 0
               WidescreenStoryboard: 0

               [Editor]
               DistanceSpacing: 1
               BeatDivisor: 4
               GridSize: 4
               TimelineZoom: 1

               [Metadata]
               Title: t
               TitleUnicode: t
               Artist: a
               ArtistUnicode: a
               Creator: c
               Version: test
               Source:
               Tags:
               BeatmapID: 0
               BeatmapSetID: -1

               [Difficulty]
               HPDrainRate: 5
               CircleSize: 4
               OverallDifficulty: 8
               ApproachRate: 9
               SliderMultiplier: 1.4
               SliderTickRate: 1

               [Events]
               //Background and Video events

               [TimingPoints]
               2354,329.670329670329,4,2,3,40,1,0
               3012,333.333333333333,4,2,3,40,1,0
               3012,-100,4,2,3,40,0,0

               [HitObjects]
               256,192,3012,2,0,B|356:192,1,145
               """.Replace("\n", "\r\n");
    }

    private static string GetUnanchoredGreenlineBeatmap()
    {
        return """
               osu file format v14

               [General]
               AudioFilename: a.mp3
               AudioLeadIn: 0
               PreviewTime: -1
               Countdown: 0
               SampleSet: Normal
               StackLeniency: 0.7
               Mode: 0
               LetterboxInBreaks: 0
               WidescreenStoryboard: 0

               [Editor]
               DistanceSpacing: 1
               BeatDivisor: 4
               GridSize: 4
               TimelineZoom: 1

               [Metadata]
               Title: t
               TitleUnicode: t
               Artist: a
               ArtistUnicode: a
               Creator: c
               Version: test
               Source:
               Tags:
               BeatmapID: 0
               BeatmapSetID: -1

               [Difficulty]
               HPDrainRate: 5
               CircleSize: 4
               OverallDifficulty: 8
               ApproachRate: 9
               SliderMultiplier: 1.4
               SliderTickRate: 1

               [Events]
               //Background and Video events

               [TimingPoints]
               0,329.670329670329,4,2,3,80,1,0
               52430,-100,4,2,3,80,0,1

               [HitObjects]
               256,192,0,1,0,0:0:0:0:
               """.Replace("\n", "\r\n");
    }

    private static string GetClosestRedlinePhaseGreenlineBeatmap()
    {
        return """
               osu file format v14

               [General]
               AudioFilename: a.mp3
               AudioLeadIn: 0
               PreviewTime: -1
               Countdown: 0
               SampleSet: Normal
               StackLeniency: 0.7
               Mode: 0
               LetterboxInBreaks: 0
               WidescreenStoryboard: 0

               [Editor]
               DistanceSpacing: 1
               BeatDivisor: 8
               GridSize: 4
               TimelineZoom: 1

               [Metadata]
               Title: t
               TitleUnicode: t
               Artist: a
               ArtistUnicode: a
               Creator: c
               Version: test
               Source:
               Tags:
               BeatmapID: 0
               BeatmapSetID: -1

               [Difficulty]
               HPDrainRate: 5
               CircleSize: 4
               OverallDifficulty: 8
               ApproachRate: 9
               SliderMultiplier: 1.8
               SliderTickRate: 1

               [Events]
               //Background and Video events

               [TimingPoints]
               51128,325.644504748982,4,2,3,65,1,0
               51290,325.644504748982,4,2,3,65,1,0
               52430,-86.9565217391304,4,2,3,80,0,1
               52592,326.086956521739,4,2,3,80,1,1

               [HitObjects]
               256,192,0,1,0,0:0:0:0:
               """.Replace("\n", "\r\n");
    }

    private static string GetSliderTailOffSnapAfterRedlineResnapBeatmap()
    {
        return """
               osu file format v14

               [General]
               AudioFilename: a.mp3
               AudioLeadIn: 0
               PreviewTime: -1
               Countdown: 0
               SampleSet: Normal
               StackLeniency: 0.7
               Mode: 0
               LetterboxInBreaks: 0
               WidescreenStoryboard: 0

               [Editor]
               DistanceSpacing: 1
               BeatDivisor: 4
               GridSize: 4
               TimelineZoom: 1

               [Metadata]
               Title: t
               TitleUnicode: t
               Artist: a
               ArtistUnicode: a
               Creator: c
               Version: test
               Source:
               Tags:
               BeatmapID: 0
               BeatmapSetID: -1

               [Difficulty]
               HPDrainRate: 5
               CircleSize: 4
               OverallDifficulty: 8
               ApproachRate: 9
               SliderMultiplier: 1.4
               SliderTickRate: 1

               [Events]
               //Background and Video events

               [TimingPoints]
               0.4,333.333333333333,4,1,0,100,1,0

               [HitObjects]
               256,192,0,2,0,B|356:192,1,35.28
               """.Replace("\n", "\r\n");
    }

    private static string GetReverseSliderMultipleSpanRedlineBeatmap()
    {
        return """
               osu file format v14

               [General]
               AudioFilename: a.mp3
               AudioLeadIn: 0
               PreviewTime: -1
               Countdown: 0
               SampleSet: Normal
               StackLeniency: 0.7
               Mode: 0
               LetterboxInBreaks: 0
               WidescreenStoryboard: 0

               [Editor]
               DistanceSpacing: 1
               BeatDivisor: 4
               GridSize: 4
               TimelineZoom: 1

               [Metadata]
               Title: t
               TitleUnicode: t
               Artist: a
               ArtistUnicode: a
               Creator: c
               Version: test
               Source:
               Tags:
               BeatmapID: 0
               BeatmapSetID: -1

               [Difficulty]
               HPDrainRate: 5
               CircleSize: 4
               OverallDifficulty: 8
               ApproachRate: 9
               SliderMultiplier: 1.4
               SliderTickRate: 1

               [Events]
               //Background and Video events

               [TimingPoints]
               0,500,4,1,0,100,1,0
               120,480,4,1,0,100,1,0
               360,360,4,1,0,100,1,0

               [HitObjects]
               256,192,0,2,0,B|356:192,3,38.2666666666667
               """.Replace("\n", "\r\n");
    }

    private static string GetReverseSliderFromRecallTheEndBeatmap()
    {
        return """
               osu file format v14

               [General]
               AudioFilename: a.mp3
               AudioLeadIn: 0
               PreviewTime: -1
               Countdown: 0
               SampleSet: Soft
               StackLeniency: 0.7
               Mode: 0
               LetterboxInBreaks: 0
               WidescreenStoryboard: 1

               [Editor]
               DistanceSpacing: 1.3
               BeatDivisor: 8
               GridSize: 4
               TimelineZoom: 5.839996

               [Metadata]
               Title: t
               TitleUnicode: t
               Artist: a
               ArtistUnicode: a
               Creator: c
               Version: test
               Source:
               Tags:
               BeatmapID: 0
               BeatmapSetID: -1

               [Difficulty]
               HPDrainRate: 5
               CircleSize: 4
               OverallDifficulty: 8
               ApproachRate: 9
               SliderMultiplier: 1.38
               SliderTickRate: 1

               [Events]
               //Background and Video events

               [TimingPoints]
               218083,-81.3008130081301,4,2,3,80,0,0
               218248,312.5,4,2,3,80,1,0
               218717,-86.7302688638335,4,2,3,80,0,0
               218873,329.67032967033,4,2,3,80,1,0

               [HitObjects]
               489,137,218717,2,0,L|441:127,3,40.393740799999996,0|0|0|0,1:0|1:0|1:0|1:0,0:0:0:0:
               """.Replace("\n", "\r\n");
    }

    private static string GetGreenlinePruningBeatmap()
    {
        return """
               osu file format v14

               [General]
               AudioFilename: a.mp3
               AudioLeadIn: 0
               PreviewTime: -1
               Countdown: 0
               SampleSet: Normal
               StackLeniency: 0.7
               Mode: 0
               LetterboxInBreaks: 0
               WidescreenStoryboard: 0

               [Editor]
               DistanceSpacing: 1
               BeatDivisor: 4
               GridSize: 4
               TimelineZoom: 1

               [Metadata]
               Title: t
               TitleUnicode: t
               Artist: a
               ArtistUnicode: a
               Creator: c
               Version: test
               Source:
               Tags:
               BeatmapID: 0
               BeatmapSetID: -1

               [Difficulty]
               HPDrainRate: 5
               CircleSize: 4
               OverallDifficulty: 8
               ApproachRate: 9
               SliderMultiplier: 1.4
               SliderTickRate: 1

               [Events]
               //Background and Video events

               [TimingPoints]
               0,500,4,1,0,100,1,0
               100,-100,4,1,0,100,0,0
               200,-50,4,1,0,100,0,0
               300,500,4,1,0,100,1,0
               400,-50,4,1,0,100,0,0

               [HitObjects]
               256,192,350,2,0,B|356:192,1,84
               """.Replace("\n", "\r\n");
    }

    private static string GetSliderBodyGreenlineBeatmap()
    {
        return """
               osu file format v14

               [General]
               AudioFilename: a.mp3
               AudioLeadIn: 0
               PreviewTime: -1
               Countdown: 0
               SampleSet: Normal
               StackLeniency: 0.7
               Mode: 0
               LetterboxInBreaks: 0
               WidescreenStoryboard: 0

               [Editor]
               DistanceSpacing: 1
               BeatDivisor: 4
               GridSize: 4
               TimelineZoom: 1

               [Metadata]
               Title: t
               TitleUnicode: t
               Artist: a
               ArtistUnicode: a
               Creator: c
               Version: test
               Source:
               Tags:
               BeatmapID: 0
               BeatmapSetID: -1

               [Difficulty]
               HPDrainRate: 5
               CircleSize: 4
               OverallDifficulty: 8
               ApproachRate: 9
               SliderMultiplier: 1.4
               SliderTickRate: 1

               [Events]
               //Background and Video events

               [TimingPoints]
               0,500,4,1,0,100,1,0
               100,-100,4,1,0,100,0,0
               200,-50,4,1,0,100,0,0
               600,-25,4,1,0,100,0,0

               [HitObjects]
               256,192,150,2,0,B|356:192,1,84
               """.Replace("\n", "\r\n");
    }

    private static string GetVolumeTickAnchoringBeatmap()
    {
        return """
               osu file format v14

               [General]
               AudioFilename: a.mp3
               AudioLeadIn: 0
               PreviewTime: -1
               Countdown: 0
               SampleSet: Normal
               StackLeniency: 0.7
               Mode: 0
               LetterboxInBreaks: 0
               WidescreenStoryboard: 0

               [Editor]
               DistanceSpacing: 1
               BeatDivisor: 4
               GridSize: 4
               TimelineZoom: 1

               [Metadata]
               Title: t
               TitleUnicode: t
               Artist: a
               ArtistUnicode: a
               Creator: c
               Version: test
               Source:
               Tags:
               BeatmapID: 0
               BeatmapSetID: -1

               [Difficulty]
               HPDrainRate: 5
               CircleSize: 4
               OverallDifficulty: 8
               ApproachRate: 9
               SliderMultiplier: 1.4
               SliderTickRate: 1

               [Events]
               //Background and Video events

               [TimingPoints]
               0,500,4,1,0,100,1,0
               449.6,-100,4,1,0,40,0,0

               [HitObjects]
               256,192,0,2,0,B|356:192,1,420
               """.Replace("\n", "\r\n");
    }

    private static string GetSampleSetOnlyGreenlineBeatmap()
    {
        return """
               osu file format v14

               [General]
               AudioFilename: a.mp3
               AudioLeadIn: 0
               PreviewTime: -1
               Countdown: 0
               SampleSet: Normal
               StackLeniency: 0.7
               Mode: 0
               LetterboxInBreaks: 0
               WidescreenStoryboard: 0

               [Editor]
               DistanceSpacing: 1
               BeatDivisor: 4
               GridSize: 4
               TimelineZoom: 1

               [Metadata]
               Title: t
               TitleUnicode: t
               Artist: a
               ArtistUnicode: a
               Creator: c
               Version: test
               Source:
               Tags:
               BeatmapID: 0
               BeatmapSetID: -1

               [Difficulty]
               HPDrainRate: 5
               CircleSize: 4
               OverallDifficulty: 8
               ApproachRate: 9
               SliderMultiplier: 1.4
               SliderTickRate: 1

               [Events]
               //Background and Video events

               [TimingPoints]
               0,500,4,1,0,100,1,0
               449.6,-100,4,2,0,100,0,0

               [HitObjects]
               256,192,0,2,0,B|356:192,1,420
               """.Replace("\n", "\r\n");
    }

    private static string CreateSandbox(string prefix)
    {
        var path = Path.Combine(Path.GetTempPath(), prefix + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string GetHeadOnlySliderSourceBeatmap()
    {
        return """
               osu file format v14

               [General]
               AudioFilename: a.mp3
               AudioLeadIn: 0
               PreviewTime: -1
               Countdown: 0
               SampleSet: Normal
               StackLeniency: 0.7
               Mode: 0
               LetterboxInBreaks: 0
               WidescreenStoryboard: 0

               [Editor]
               DistanceSpacing: 1
               BeatDivisor: 4
               GridSize: 4
               TimelineZoom: 1

               [Metadata]
               Title: t
               TitleUnicode: t
               Artist: a
               ArtistUnicode: a
               Creator: c
               Version: test
               Source:
               Tags:
               BeatmapID: 0
               BeatmapSetID: -1

               [Difficulty]
               HPDrainRate: 5
               CircleSize: 4
               OverallDifficulty: 8
               ApproachRate: 9
               SliderMultiplier: 1.4
               SliderTickRate: 1

               [Events]
               //Background and Video events

               [TimingPoints]
               0,500,4,1,0,100,1,0

               [HitObjects]
               256,192,0,1,2,0:0:0:0:
               """.Replace("\n", "\r\n");
    }

    private static string GetHeadOnlySliderTargetBeatmap()
    {
        return """
               osu file format v14

               [General]
               AudioFilename: a.mp3
               AudioLeadIn: 0
               PreviewTime: -1
               Countdown: 0
               SampleSet: Normal
               StackLeniency: 0.7
               Mode: 0
               LetterboxInBreaks: 0
               WidescreenStoryboard: 0

               [Editor]
               DistanceSpacing: 1
               BeatDivisor: 4
               GridSize: 4
               TimelineZoom: 1

               [Metadata]
               Title: t
               TitleUnicode: t
               Artist: a
               ArtistUnicode: a
               Creator: c
               Version: test
               Source:
               Tags:
               BeatmapID: 0
               BeatmapSetID: -1

               [Difficulty]
               HPDrainRate: 5
               CircleSize: 4
               OverallDifficulty: 8
               ApproachRate: 9
               SliderMultiplier: 1.4
               SliderTickRate: 1

               [Events]
               //Background and Video events

               [TimingPoints]
               0,500,4,1,0,100,1,0

               [HitObjects]
               256,192,0,2,0,B|356:192,1,84
               """.Replace("\n", "\r\n");
    }
}
