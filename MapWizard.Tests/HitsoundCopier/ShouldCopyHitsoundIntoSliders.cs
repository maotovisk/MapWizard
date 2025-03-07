using System.Reflection;
using MapWizard.BeatmapParser;
using MapWizard.Tools.HitSoundCopier;

namespace MapWizard.Tests.HitsoundCopier;


public class ShouldCopyHitsoundIntoSliders
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
        using var reader = new StreamReader(stream);
        var beatmapString = reader.ReadToEnd();
        var hsOptions = new HitSoundCopierOptions()
        {
            CopySampleAndVolumeChanges = true,
            CopySliderBodySounds = true,
            Leniency = 5,
            OvewriteEverything = true,
            OverwriteMuting = false
        };
        
        // Act
        var beatmap = Beatmap.Decode(beatmapString);
        
        var copyedBeatmap = HitSoundCopier.CopyFromBeatmap(beatmap, beatmap, hsOptions);
        
        var encodedBeatmap = copyedBeatmap.Encode();
        
        // Assert
        Assert.Equal(beatmapString, encodedBeatmap);
    }
}