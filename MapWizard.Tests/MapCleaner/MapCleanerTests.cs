using BeatmapParser;
using BeatmapParser.HitObjects;
using BeatmapParser.TimingPoints;
using MapWizard.Tools.MapCleaner;

namespace MapWizard.Tests.MapCleaner;

public class MapCleanerTests
{
    [Fact]
    public void CleanBeatmap_UnsnapedObject_ResnapsToNearestTick()
    {
        // Arrange
        var beatmap = Beatmap.Decode(GetSimpleResnapBeatmap());
        var firstObject = beatmap.HitObjects.Objects[0];

        var options = new MapCleanerOptions
        {
            ResnapObjects = true,
            RemoveUnusedInheritedTimingPoints = false,
            SnapDivisors = ["1/2", "1/4", "1/8"]
        };

        // Act
        var result = MapWizard.Tools.MapCleaner.MapCleaner.CleanBeatmap(beatmap, options);

        // Assert
        Assert.Equal(TimeSpan.FromMilliseconds(125), firstObject.Time);
        Assert.True(result.ObjectsResnapped >= 1);
    }

    [Fact]
    public void CleanBeatmap_ObjectWithinTenMillisecondsAfterRedline_UsesFutureRedlineForSnap()
    {
        // Arrange
        var beatmap = Beatmap.Decode(GetFutureRedlineTestBeatmap());
        var targetObject = beatmap.HitObjects.Objects[0];

        var options = new MapCleanerOptions
        {
            ResnapObjects = true,
            RemoveUnusedInheritedTimingPoints = false,
            AnalyzeSamples = false,
            SnapDivisors = ["1/4"]
        };

        // Act
        MapWizard.Tools.MapCleaner.MapCleaner.CleanBeatmap(beatmap, options);

        // Assert
        Assert.Equal(TimeSpan.FromMilliseconds(1005), targetObject.Time);
    }

    [Fact]
    public void CleanBeatmap_UnusedInheritedTimingPoint_RemovesIt()
    {
        // Arrange
        var beatmap = Beatmap.Decode(GetUnusedInheritedTimingPointBeatmap());
        Assert.NotNull(beatmap.TimingPoints);
        var beforeInheritedCount = beatmap.TimingPoints.TimingPointList.Count(x => x is InheritedTimingPoint);

        var options = new MapCleanerOptions
        {
            ResnapObjects = false,
            AnalyzeSamples = false,
            RemoveUnusedInheritedTimingPoints = true
        };

        // Act
        var result = MapWizard.Tools.MapCleaner.MapCleaner.CleanBeatmap(beatmap, options);

        // Assert
        var afterInheritedCount = beatmap.TimingPoints.TimingPointList.Count(x => x is InheritedTimingPoint);
        Assert.Equal(beforeInheritedCount - 1, afterInheritedCount);
        Assert.Equal(1, result.InheritedTimingPointsRemoved);
    }

    [Fact]
    public void CleanBeatmap_UnsnapedGreenline_ResnapsIt()
    {
        // Arrange
        var beatmap = Beatmap.Decode(GetGreenlineResnapBeatmap());
        Assert.NotNull(beatmap.TimingPoints);
        var greenline = beatmap.TimingPoints.TimingPointList.OfType<InheritedTimingPoint>().First();

        var options = new MapCleanerOptions
        {
            ResnapObjects = false,
            ResnapGreenLines = true,
            RemoveUnusedInheritedTimingPoints = false,
            SnapDivisors = ["1/4"]
        };

        // Act
        var result = MapWizard.Tools.MapCleaner.MapCleaner.CleanBeatmap(beatmap, options);

        // Assert
        Assert.Equal(TimeSpan.FromMilliseconds(125), greenline.Time);
        Assert.Equal(1, result.GreenLinesResnapped);
    }

    [Fact]
    public void CleanBeatmap_ReverseSlider_ResnapsByFirstSlideDuration()
    {
        // Arrange
        var beatmap = Beatmap.Decode(GetReverseSliderResnapBeatmap());
        var slider = beatmap.HitObjects.Objects.OfType<Slider>().Single();

        var options = new MapCleanerOptions
        {
            ResnapObjects = true,
            RemoveUnusedInheritedTimingPoints = false,
            SnapDivisors = ["1/4"]
        };

        // Act
        var result = MapWizard.Tools.MapCleaner.MapCleaner.CleanBeatmap(beatmap, options);

        // Assert
        Assert.Equal((uint)2, slider.Slides);
        Assert.Equal(TimeSpan.FromMilliseconds(250), slider.EndTime);
        Assert.InRange(slider.Length, 34.999, 35.001);
        Assert.Equal(1, result.SliderEndsResnapped);
    }

    private static string GetFutureRedlineTestBeatmap()
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
               Bookmarks: 1001

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
               1005,400,4,1,0,100,1,0

               [HitObjects]
               256,192,1001,1,0,0:0:0:0:
               """.Replace("\n", "\r\n");
    }

    private static string GetSimpleResnapBeatmap()
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
               256,192,126,1,0,0:0:0:0:
               """.Replace("\n", "\r\n");
    }

    private static string GetUnusedInheritedTimingPointBeatmap()
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
               100,-100,4,1,0,60,0,0
               200,-100,4,1,0,70,0,0

               [HitObjects]
               256,192,300,1,0,0:0:0:0:
               """.Replace("\n", "\r\n");
    }

    private static string GetGreenlineResnapBeatmap()
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
               101,-100,4,1,0,60,0,0

               [HitObjects]
               256,192,300,1,0,0:0:0:0:
               """.Replace("\n", "\r\n");
    }

    private static string GetReverseSliderResnapBeatmap()
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
               256,192,0,2,0,B|356:192,2,50.4
               """.Replace("\n", "\r\n");
    }
}
