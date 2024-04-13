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
}
