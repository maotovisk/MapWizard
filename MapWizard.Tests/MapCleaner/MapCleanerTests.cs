using BeatmapParser;
using BeatmapParser.HitObjects;
using BeatmapParser.TimingPoints;
using MapWizard.Tools.HitSounds.Copier;
using MapWizard.Tools.MapCleaner;

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
        Assert.Equal(TimeSpan.FromMilliseconds(625), secondRedline.Time);
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
        Assert.Equal(TimeSpan.FromMilliseconds(375), redlines[1].Time);
        Assert.Equal(TimeSpan.FromMilliseconds(878), redlines[2].Time);
        Assert.Equal(2, result.TimingPointsResnapped);
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
        Assert.Equal(TimeSpan.FromMilliseconds(400), inheritedTimingPoints[0].Time);
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
        Assert.Equal(TimeSpan.FromMilliseconds(200), inheritedTimingPoints[0].Time);
        Assert.Equal(2.0, inheritedTimingPoints[0].SliderVelocity, precision: 3);
        Assert.Equal(2, result.GreenLinesRemoved);
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
            Assert.Single(Directory.GetFiles(backupDirectory, "*.osu"));
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
               101,-100,4,1,0,100,0,0
               626,500,4,1,0,100,1,0

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
               378,400,4,1,0,100,1,0
               876,400,4,1,0,100,1,0

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
