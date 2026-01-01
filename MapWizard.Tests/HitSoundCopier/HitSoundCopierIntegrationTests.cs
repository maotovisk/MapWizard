using System.Reflection;
using BeatmapParser;
using MapWizard.Tools.HitSounds.Copier;

namespace MapWizard.Tests.HitSoundCopier;


public class HitSoundCopierIntegrationTests
{
    [Fact] public void CopyHsIntoSliders_InputIsCircleOnlyDiff_ReturnsTrue()
    {
        // todo(hs_copier): Implement test
        Assert.True(true);
    }

    [Fact]
    public void CopyHsIntoSameDiff_InputIsVanillaBeatmap_ReturnsTrue()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("MapWizard.Tests.Resources.test7.osu");
        if (stream == null) return;
        
        using var reader = new StreamReader(stream);
        var beatmapString = reader.ReadToEnd();
        var hsOptions = new HitSoundCopierOptions()
        {
            CopySampleAndVolumeChanges = true,
            CopySliderBodySounds = true,
            Leniency = 5,
            OverwriteEverything = true,
            OverwriteMuting = false
        };
        
        // Act
        var beatmap = Beatmap.Decode(beatmapString);
        
        var copyedBeatmap = Tools.HitSounds.Copier.HitSoundCopier.CopyFromBeatmap(beatmap, beatmap, hsOptions);
        
        var encodedBeatmap = copyedBeatmap.Encode();
        
        // Assert
        Assert.Equal(beatmapString, encodedBeatmap);
    }
}