using BeatmapParser;
using MapWizard.Tools.MapCleaner.Snapping;

namespace MapWizard.Tests.MapCleaner;

public class StableSnapEngineTests
{
    [Fact]
    public void ParseDivisors_WithOnlyInvalidValues_FallsBackToDefaultDivisors()
    {
        var divisors = StableSnapEngine.ParseDivisors(["", "abc", "1", "0/4", "2/0"]);

        Assert.Collection(divisors,
            divisor =>
            {
                Assert.Equal(1, divisor.Numerator);
                Assert.Equal(8, divisor.Denominator);
            },
            divisor =>
            {
                Assert.Equal(1, divisor.Numerator);
                Assert.Equal(12, divisor.Denominator);
            });
    }

    [Fact]
    public void ParseDivisors_RemovesDuplicatesAndSortsByDenominatorThenNumerator()
    {
        var divisors = StableSnapEngine.ParseDivisors(["1/12", "1/8", "1/12", "3/16", "1/16"]);

        Assert.Collection(divisors,
            divisor =>
            {
                Assert.Equal(1, divisor.Numerator);
                Assert.Equal(8, divisor.Denominator);
            },
            divisor =>
            {
                Assert.Equal(1, divisor.Numerator);
                Assert.Equal(12, divisor.Denominator);
            },
            divisor =>
            {
                Assert.Equal(1, divisor.Numerator);
                Assert.Equal(16, divisor.Denominator);
            },
            divisor =>
            {
                Assert.Equal(3, divisor.Numerator);
                Assert.Equal(16, divisor.Denominator);
            });
    }

    [Theory]
    [InlineData(1.49, 1)]
    [InlineData(1.5, 2)]
    [InlineData(-1.49, -1)]
    [InlineData(-1.5, -2)]
    public void StableRound_MatchesStableRoundingBehavior(double value, int expected)
    {
        Assert.Equal(expected, StableSnapEngine.StableRound(value));
    }

    [Fact]
    public void SnapRelativeMilliseconds_WhenEquidistant_PrefersEarlierCandidate()
    {
        var snapped = StableSnapEngine.SnapRelativeMilliseconds(62.5, 0, 500, [new SnapDivisor(1, 4)]);

        Assert.Equal(125, snapped);
    }

    [Fact]
    public void SnapMilliseconds_UsesForwardRedlineWhenWithinWindow()
    {
        var beatmap = Beatmap.Decode(GetForwardWindowBeatmap());
        var divisors = StableSnapEngine.ParseDivisors(["1/4"]);

        var withForwardWindow = StableSnapEngine.SnapMilliseconds(985, beatmap.TimingPoints, divisors, forwardRedlineWindowMs: 10);
        var withoutForwardWindow = StableSnapEngine.SnapMilliseconds(985, beatmap.TimingPoints, divisors, forwardRedlineWindowMs: 4);

        Assert.Equal(990, withForwardWindow);
        Assert.Equal(990, withoutForwardWindow);
    }

    private static string GetForwardWindowBeatmap()
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
               990,500,4,1,0,100,1,0

               [HitObjects]
               256,192,0,1,0,0:0:0:0:
               """.Replace("\n", "\r\n");
    }
}
