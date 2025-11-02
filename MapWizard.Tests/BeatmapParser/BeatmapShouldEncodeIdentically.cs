using System;
using System.Collections.Generic;
using System.Reflection;
using MapWizard.BeatmapParser;

namespace MapWizard.Tests.BeatmapParser;

public class BeatmapShouldEncodeIdentically
{
    private static List<string> GetSectionLines(string content, string sectionName)
    {
        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var start = Array.FindIndex(lines, l => l.Trim().Equals($"[{sectionName}]", StringComparison.OrdinalIgnoreCase));
        if (start == -1) return new List<string>();

        List<string> sectionLines = [];
        for (int i = start + 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.StartsWith('[')) break;
            if (string.IsNullOrWhiteSpace(line)) continue;
            sectionLines.Add(line.Trim());
        }

        return sectionLines;
    }

    private static void AssertSectionEquals(string original, string encoded, string sectionName)
    {
        var origSection = GetSectionLines(original, sectionName);
        var encSection = GetSectionLines(encoded, sectionName);
        Assert.Equal(origSection, encSection);
    }

    [Fact]
    public void Encode_InputIsBeatmapString_ReturnsTrue()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("MapWizard.Tests.Resources.test5.osu");
        if (stream == null) return;
        
        using var reader = new StreamReader(stream);
        var beatmapString = reader.ReadToEnd();
        
        // Act
        var beatmap = Beatmap.Decode(beatmapString);
        var encodedBeatmap = beatmap.Encode();
        
        // Assert
        Assert.Equal(beatmapString, encodedBeatmap);
    }
    
    [Fact]
    public void Encode_InputIsVanillaBeatmapString_ReturnsTrue()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("MapWizard.Tests.Resources.test3.osu");
        if (stream == null) return;
        
        using var reader = new StreamReader(stream);
        var beatmapString = reader.ReadToEnd();
        
        // Act
        var beatmap = Beatmap.Decode(beatmapString);
        var encodedBeatmap = beatmap.Encode();
        
        // Assert
        Assert.Equal(beatmapString, encodedBeatmap);
    }
    
    [Fact]
    public void Encode_InputIsOldStyleBeatmapString_ReturnsTrue()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("MapWizard.Tests.Resources.test6.osu");
        if (stream == null) return;
        
        using var reader = new StreamReader(stream);
        var beatmapString = reader.ReadToEnd();
        
        // Act
        var beatmap = Beatmap.Decode(beatmapString);
        var encodedBeatmap = beatmap.Encode();
        
        // Assert
        Assert.Equal(beatmapString, encodedBeatmap);
    }
    
    [Fact]
    public void Encode_InputIsColouredSliderBeatmapString_ReturnsTrue()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("MapWizard.Tests.Resources.test7.osu");
        if (stream == null) return;
        
        using var reader = new StreamReader(stream);
        var beatmapString = reader.ReadToEnd();
        
        // Act
        var beatmap = Beatmap.Decode(beatmapString);
        var encodedBeatmap = beatmap.Encode();
        // Assert
        Assert.Equal(beatmapString, encodedBeatmap);
    }

    [Fact]
    public void Encode_InputIsLazerBeatmap1String_ReturnsTrue()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("MapWizard.Tests.Resources.test9.osu");
        if (stream == null) return;
        
        using var reader = new StreamReader(stream);
        var beatmapString = reader.ReadToEnd();

        // Act
        var beatmap = Beatmap.Decode(beatmapString);
        var encodedBeatmap = beatmap.Encode();

        // Assert
        AssertSectionEquals(beatmapString, encodedBeatmap, "General");
        AssertSectionEquals(beatmapString, encodedBeatmap, "Editor");
        AssertSectionEquals(beatmapString, encodedBeatmap, "Colours");
        AssertSectionEquals(beatmapString, encodedBeatmap, "Metadata");
        AssertSectionEquals(beatmapString, encodedBeatmap, "TimingPoints");
        AssertSectionEquals(beatmapString, encodedBeatmap, "HitObjects");
    }
    
    [Fact]
    public void Encode_InputIsLazerBeatmap2String_ReturnsTrue()
    {
        // Arrange
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("MapWizard.Tests.Resources.test10.osu");
        if (stream == null) return;
        
        using var reader = new StreamReader(stream);
        var beatmapString = reader.ReadToEnd();

        // Act
        var beatmap = Beatmap.Decode(beatmapString);
        var encodedBeatmap = beatmap.Encode();

        // Assert
        AssertSectionEquals(beatmapString, encodedBeatmap, "General");
        AssertSectionEquals(beatmapString, encodedBeatmap, "Editor");
        AssertSectionEquals(beatmapString, encodedBeatmap, "Colours");
        AssertSectionEquals(beatmapString, encodedBeatmap, "Metadata");
        AssertSectionEquals(beatmapString, encodedBeatmap, "TimingPoints");
        AssertSectionEquals(beatmapString, encodedBeatmap, "HitObjects");
    }
}
