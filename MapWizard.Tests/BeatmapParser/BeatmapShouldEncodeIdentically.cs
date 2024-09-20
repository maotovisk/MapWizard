using System.Reflection;
using MapWizard.BeatmapParser;

namespace MapWizard.Tests.BeatmapParser;

public class BeatmapShouldEncodeIdentically
{
    [Fact]
    public void Encode_InputIsBeatmapString_ReturnsTrue()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("MapWizard.Tests.Resources.test5.osu");
        using var reader = new StreamReader(stream);
        var beatmapString = reader.ReadToEnd();
        
        // Act
        var encodedBeatmap = Beatmap.Decode(beatmapString).Encode();
        
        // Assert
        Assert.Equal(beatmapString, encodedBeatmap);
    }
    
    [Fact]
    public void Encode_InputIsVanillaBeatmapString_ReturnsTrue()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("MapWizard.Tests.Resources.test3.osu");
        using var reader = new StreamReader(stream);
        var beatmapString = reader.ReadToEnd();
        
        // Act
        var encodedBeatmap = Beatmap.Decode(beatmapString).Encode();
        
        // Assert
        Assert.Equal(beatmapString, encodedBeatmap);
    }
    
    [Fact]
    public void Encode_InputIsOldStyleBeatmapString_ReturnsTrue()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("MapWizard.Tests.Resources.test6.osu");
        using var reader = new StreamReader(stream);
        var beatmapString = reader.ReadToEnd();
        
        // Act
        var encodedBeatmap = Beatmap.Decode(beatmapString).Encode();
        
        // Assert
        Assert.Equal(beatmapString, encodedBeatmap);
    }
}
