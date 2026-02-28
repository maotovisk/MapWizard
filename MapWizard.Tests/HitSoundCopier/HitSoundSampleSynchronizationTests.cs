using BeatmapParser;
using BeatmapParser.HitObjects;
using MapWizard.Tools.HitSounds.Copier;

namespace MapWizard.Tests.HitSoundCopier;

public class HitSoundSampleSynchronizationTests
{
    [Fact]
    public void CopyAcrossMapsets_WithConflictingIndexedSample_CreatesNewIndexAndCopiesSamples()
    {
        var sandboxRoot = CreateSandbox();
        var previousXdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        Environment.SetEnvironmentVariable("XDG_DATA_HOME", sandboxRoot);

        try
        {
            var sourceDir = Directory.CreateDirectory(Path.Combine(sandboxRoot, "source"));
            var targetDir = Directory.CreateDirectory(Path.Combine(sandboxRoot, "target"));
            var sourcePath = Path.Combine(sourceDir.FullName, "source.osu");
            var targetPath = Path.Combine(targetDir.FullName, "target.osu");

            File.WriteAllText(sourcePath, BuildBeatmap(sampleIndex: 2, customFileName: "custom-hit.wav", hitSound: 8));
            File.WriteAllText(targetPath, BuildBeatmap(sampleIndex: 1, customFileName: string.Empty, hitSound: 0));
            File.WriteAllText(Path.Combine(sourceDir.FullName, "normal-hitclap2.wav"), "source-indexed");
            File.WriteAllText(Path.Combine(sourceDir.FullName, "custom-hit.wav"), "source-custom");
            File.WriteAllText(Path.Combine(targetDir.FullName, "normal-hitclap2.wav"), "target-conflict");

            var options = new HitSoundCopierOptions
            {
                CopyUsedSamplesIfDifferentMapset = true
            };

            MapWizard.Tools.HitSounds.Copier.HitSoundCopier.CopyFromBeatmapToTarget(sourcePath, [targetPath], options);

            Assert.True(File.Exists(Path.Combine(targetDir.FullName, "normal-hitclap3.wav")));
            Assert.True(File.Exists(Path.Combine(targetDir.FullName, "custom-hit.wav")));
            Assert.Equal("source-indexed", File.ReadAllText(Path.Combine(targetDir.FullName, "normal-hitclap3.wav")));
            Assert.Equal("source-custom", File.ReadAllText(Path.Combine(targetDir.FullName, "custom-hit.wav")));

            var outputBeatmap = Beatmap.Decode(File.ReadAllText(targetPath));
            var sampleIndices = outputBeatmap.TimingPoints!.TimingPointList
                .Select(x => (int)x.SampleIndex)
                .Distinct()
                .ToList();
            Assert.Contains(3, sampleIndices);
            Assert.DoesNotContain(2, sampleIndices);

            var circle = Assert.IsType<Circle>(outputBeatmap.HitObjects.Objects[0]);
            Assert.Equal("custom-hit.wav", circle.HitSounds.SampleData.FileName);
        }
        finally
        {
            Environment.SetEnvironmentVariable("XDG_DATA_HOME", previousXdgDataHome);
            Directory.Delete(sandboxRoot, recursive: true);
        }
    }

    [Fact]
    public void CopyAcrossMapsets_WithMatchingIndexedSamples_ReusesExistingFilesWithoutRemap()
    {
        var sandboxRoot = CreateSandbox();
        var previousXdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        Environment.SetEnvironmentVariable("XDG_DATA_HOME", sandboxRoot);

        try
        {
            var sourceDir = Directory.CreateDirectory(Path.Combine(sandboxRoot, "source"));
            var targetDir = Directory.CreateDirectory(Path.Combine(sandboxRoot, "target"));
            var sourcePath = Path.Combine(sourceDir.FullName, "source.osu");
            var targetPath = Path.Combine(targetDir.FullName, "target.osu");

            File.WriteAllText(sourcePath, BuildBeatmap(sampleIndex: 2, customFileName: string.Empty, hitSound: 8));
            File.WriteAllText(targetPath, BuildBeatmap(sampleIndex: 1, customFileName: string.Empty, hitSound: 0));
            File.WriteAllText(Path.Combine(sourceDir.FullName, "normal-hitclap2.wav"), "shared-indexed");
            File.WriteAllText(Path.Combine(targetDir.FullName, "normal-hitclap2.wav"), "shared-indexed");

            var options = new HitSoundCopierOptions
            {
                CopyUsedSamplesIfDifferentMapset = true
            };

            MapWizard.Tools.HitSounds.Copier.HitSoundCopier.CopyFromBeatmapToTarget(sourcePath, [targetPath], options);

            Assert.False(File.Exists(Path.Combine(targetDir.FullName, "normal-hitclap3.wav")));
            Assert.Equal("shared-indexed", File.ReadAllText(Path.Combine(targetDir.FullName, "normal-hitclap2.wav")));

            var outputBeatmap = Beatmap.Decode(File.ReadAllText(targetPath));
            var sampleIndices = outputBeatmap.TimingPoints!.TimingPointList
                .Select(x => (int)x.SampleIndex)
                .Distinct()
                .ToList();
            Assert.Contains(2, sampleIndices);
        }
        finally
        {
            Environment.SetEnvironmentVariable("XDG_DATA_HOME", previousXdgDataHome);
            Directory.Delete(sandboxRoot, recursive: true);
        }
    }

    private static string BuildBeatmap(int sampleIndex, string customFileName, int hitSound)
    {
        var sampleField = customFileName.Length == 0 ? "0:0:0:0:" : $"0:0:0:0:{customFileName}";
        return $$"""
                 osu file format v14

                 [General]
                 AudioFilename: audio.mp3
                 PreviewTime: -1
                 Mode: 0

                 [Metadata]
                 Title:Sample Sync
                 TitleUnicode:Sample Sync
                 Artist:MapWizard
                 ArtistUnicode:MapWizard
                 Creator:MapWizard
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
                 //Background and Video events
                 //Break Periods

                 [TimingPoints]
                 0,500,4,2,1,70,1,0
                 0,-100,4,2,{{sampleIndex}},70,0,0

                 [HitObjects]
                 64,192,1000,1,{{hitSound}},{{sampleField}}
                 """;
    }

    private static string CreateSandbox()
    {
        var path = Path.Combine(Path.GetTempPath(), "mapwizard-hs-sync-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
