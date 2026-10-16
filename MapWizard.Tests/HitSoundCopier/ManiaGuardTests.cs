using BeatmapParser;
using MapWizard.Tools.HitSounds.Copier;

namespace MapWizard.Tests.HitSoundCopier;

public class ManiaGuardTests
{
    private const string ManiaMap = """
        osu file format v14

        [General]
        AudioFilename: audio.mp3
        Mode: 3

        [Metadata]
        Title:Test
        TitleUnicode:Test
        Artist:Test
        ArtistUnicode:Test
        Creator:Test
        Version:Test
        Source:
        Tags:
        BeatmapID:0
        BeatmapSetID:-1

        [Difficulty]
        HPDrainRate:5
        CircleSize:4
        OverallDifficulty:8
        ApproachRate:9
        SliderMultiplier:1.4
        SliderTickRate:1

        [Events]

        [TimingPoints]
        0,500,4,1,1,70,1,0

        [HitObjects]
        64,192,1000,1,0,0:0:0:0:
        """;

    [Fact]
    public void CopyFromBeatmap_WithManiaSource_ReturnsNotSupportedFailure()
    {
        var source = Beatmap.Decode(ManiaMap);
        var target = Beatmap.Decode(ManiaMap.Replace("Mode: 3", "Mode: 0"));

        var result = Tools.HitSounds.Copier.HitSoundCopier.CopyFromBeatmap(source, target, new HitSoundCopierOptions());

        Assert.True(result.IsFailure);
        Assert.Equal("hitsounds.mania_not_supported", result.Error.Code);
        Assert.Equal("Hit Sound copying for mania is currently WIP and not supported yet.", result.Error.Message);
    }

    [Fact]
    public void CopyFromBeatmapToTarget_WithManiaSource_FailsAndLeavesTargetUntouched()
    {
        var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "mapwizard-guard-" + Guid.NewGuid().ToString("N"))).FullName;
        try
        {
            var sourcePath = Path.Combine(directory, "mania.osu");
            var targetPath = Path.Combine(directory, "std.osu");
            var targetContent = ManiaMap.Replace("Mode: 3", "Mode: 0");
            File.WriteAllText(sourcePath, ManiaMap);
            File.WriteAllText(targetPath, targetContent);

            var result = Tools.HitSounds.Copier.HitSoundCopier.CopyFromBeatmapToTarget(sourcePath, [targetPath], new HitSoundCopierOptions());

            Assert.True(result.IsFailure);
            Assert.Equal("hitsounds.mania_not_supported", result.Error.Code);
            Assert.Equal(targetContent, File.ReadAllText(targetPath));
            Assert.Equal(new[] { sourcePath, targetPath }.Order(), Directory.GetFiles(directory).Order());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void CopyFromBeatmapToTarget_WithMissingFile_ReturnsFileNotFound()
    {
        var result = Tools.HitSounds.Copier.HitSoundCopier.CopyFromBeatmapToTarget(
            "/does/not/exist.osu", ["/neither/does/this.osu"], new HitSoundCopierOptions());

        Assert.True(result.IsFailure);
        Assert.Equal("hitsounds.file_not_found", result.Error.Code);
    }
}
