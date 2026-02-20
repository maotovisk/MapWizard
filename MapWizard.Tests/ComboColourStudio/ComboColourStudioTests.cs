using System.Drawing;
using BeatmapParser;
using BeatmapParser.Colours;
using MapWizard.Tools.ComboColourStudio;
using ComboColourStudioTool = MapWizard.Tools.ComboColourStudio.ComboColourStudio;

namespace MapWizard.Tests.ComboColourStudio;

public class ComboColourStudioTests
{
    [Fact]
    public void ApplyProjectToBeatmap_SetsExpectedComboOffsets()
    {
        // Arrange
        var beatmap = Beatmap.Decode(BuildBeatmap(new[]
        {
            "64,192,1000,5,0,0:0:0:0:",
            "128,192,1500,1,0,0:0:0:0:",
            "192,192,2000,5,0,0:0:0:0:"
        }));

        var project = new ComboColourProject
        {
            ComboColours =
            [
                new ComboColour(1, Color.FromArgb(255, 255, 0, 0)),
                new ComboColour(2, Color.FromArgb(255, 0, 255, 0)),
                new ComboColour(3, Color.FromArgb(255, 0, 0, 255)),
                new ComboColour(4, Color.FromArgb(255, 255, 255, 0))
            ],
            ColourPoints =
            [
                new ComboColourPoint
                {
                    Time = 0,
                    ColourSequence = [0, 1],
                    Mode = ColourPointMode.Normal
                }
            ],
            MaxBurstLength = 1
        };

        // Act
        ComboColourStudioTool.ApplyProjectToBeatmap(beatmap: beatmap, project: project, options: new ComboColourStudioOptions
        {
            UpdateComboColoursSection = true,
            OverrideHitObjectColourShifts = true,
            CreateColoursSectionIfMissing = true,
            CreateBackupBeforeWrite = false
        });

        // Assert
        Assert.Equal<uint>(3, beatmap.HitObjects.Objects[0].ComboOffset);
        Assert.Equal<uint>(0, beatmap.HitObjects.Objects[2].ComboOffset);
    }

    [Fact]
    public void ApplyProjectToBeatmap_ResetsExistingNoteComboOffsetsBeforeApplying()
    {
        // Arrange
        var beatmap = Beatmap.Decode(BuildBeatmap(new[]
        {
            "64,192,1000,5,0,0:0:0:0:",
            "128,192,1500,1,0,0:0:0:0:",
            "192,192,2000,5,0,0:0:0:0:"
        }));
        beatmap.HitObjects.Objects[1].ComboOffset = 2;
        beatmap.HitObjects.Objects[1].NewCombo = false;

        var project = new ComboColourProject
        {
            ComboColours =
            [
                new ComboColour(1, Color.FromArgb(255, 255, 0, 0)),
                new ComboColour(2, Color.FromArgb(255, 0, 255, 0))
            ],
            ColourPoints =
            [
                new ComboColourPoint
                {
                    Time = 0,
                    ColourSequence = [0, 1],
                    Mode = ColourPointMode.Normal
                }
            ],
            MaxBurstLength = 1
        };

        // Act
        ComboColourStudioTool.ApplyProjectToBeatmap(beatmap: beatmap, project: project, options: new ComboColourStudioOptions
        {
            UpdateComboColoursSection = true,
            OverrideHitObjectColourShifts = true,
            CreateColoursSectionIfMissing = true,
            CreateBackupBeforeWrite = false
        });

        // Assert
        Assert.Equal<uint>(0, beatmap.HitObjects.Objects[1].ComboOffset);
    }

    [Fact]
    public void ExtractColourHaxFromBeatmap_AndReapply_PreservesComboSettings()
    {
        // Arrange
        var beatmapContent = BuildBeatmap(new[]
        {
            "64,192,1000,53,0,0:0:0:0:",
            "128,192,1500,1,0,0:0:0:0:",
            "192,192,2000,5,0,0:0:0:0:",
            "256,192,2500,1,0,0:0:0:0:",
            "320,192,3000,21,0,0:0:0:0:"
        });

        var original = Beatmap.Decode(beatmapContent);
        var reapplied = Beatmap.Decode(beatmapContent);

        // Act
        var extracted = ComboColourStudioTool.ExtractColourHaxFromBeatmap(original, maxBurstLength: 1);
        ComboColourStudioTool.ApplyProjectToBeatmap(extracted, reapplied, new ComboColourStudioOptions
        {
            UpdateComboColoursSection = true,
            OverrideHitObjectColourShifts = true,
            CreateColoursSectionIfMissing = true,
            CreateBackupBeforeWrite = false
        });

        // Assert
        var originalPairs = original.HitObjects.Objects.Select(o => (o.NewCombo, o.ComboOffset)).ToArray();
        var reappliedPairs = reapplied.HitObjects.Objects.Select(o => (o.NewCombo, o.ComboOffset)).ToArray();

        Assert.Equal(originalPairs, reappliedPairs);
    }

    [Fact]
    public void GenerateProminentColours_DarkImage_AvoidsLowLuminosityColours()
    {
        var darkColour = Color.FromArgb(255, 15, 20, 25);
        var readable = ComboColourStudioTool.MakeColourReadableForGameplay(darkColour);

        Assert.True(GetPerceivedLuminosity(readable) > 50d);
    }

    [Fact]
    public void MakeColourReadableForGameplay_BrightColour_AvoidsHighLuminosityColours()
    {
        var brightColour = Color.FromArgb(255, 250, 250, 250);
        var readable = ComboColourStudioTool.MakeColourReadableForGameplay(brightColour);

        Assert.True(GetPerceivedLuminosity(readable) < 220d);
    }

    [Fact]
    public void MakeColourReadableForGameplay_ColourWithinRange_RemainsUnchanged()
    {
        var neutralColour = Color.FromArgb(255, 88, 155, 205);
        var readable = ComboColourStudioTool.MakeColourReadableForGameplay(neutralColour);

        Assert.Equal(neutralColour.ToArgb(), readable.ToArgb());
    }

    [Fact]
    public void ExtractColourHaxFromBeatmap_BreakInPattern_CreatesPointAfterBreak()
    {
        var beatmap = Beatmap.Decode(BuildBeatmap(
            hitObjects:
            [
                "64,192,1000,53,0,0:0:0:0:",
                "128,192,1500,53,0,0:0:0:0:",
                "192,192,4500,53,0,0:0:0:0:",
                "256,192,5000,53,0,0:0:0:0:"
            ],
            eventLines:
            [
                "//Background and Video events",
                "0,0,\"bg.jpg\",0,0",
                "2,2000,4000"
            ]));

        var project = ComboColourStudioTool.ExtractColourHaxFromBeatmap(beatmap, maxBurstLength: 1);

        Assert.True(project.ColourPoints.Count >= 2);
        Assert.Contains(project.ColourPoints, point => point.Time >= 4500);
    }

    private static string BuildBeatmap(IEnumerable<string> hitObjects, IEnumerable<string>? eventLines = null)
    {
        var events = eventLines?.ToArray() ??
                     [
                         "//Background and Video events",
                         "0,0,\"bg.jpg\",0,0"
                     ];

        return $"""
osu file format v14

[General]
AudioFilename: test.mp3
Mode: 0

[Metadata]
Title:Test
Artist:Test
Creator:Test
Version:Test

[Difficulty]
HPDrainRate:5
CircleSize:4
OverallDifficulty:5
ApproachRate:5
SliderMultiplier:1.4
SliderTickRate:1

[Events]
{string.Join("\n", events)}

[TimingPoints]
0,500,4,2,0,50,1,0

[Colours]
Combo1 : 255,0,0
Combo2 : 0,255,0
Combo3 : 0,0,255
Combo4 : 255,255,0

[HitObjects]
{string.Join("\n", hitObjects)}
""";
    }

    private static double GetPerceivedLuminosity(Color colour)
    {
        return 0.2126d * colour.R + 0.7152d * colour.G + 0.0722d * colour.B;
    }
}
