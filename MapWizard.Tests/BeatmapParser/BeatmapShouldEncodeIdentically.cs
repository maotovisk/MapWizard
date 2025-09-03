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
        static List<string> GetSectionLines(string content, string sectionName)
        {
            var lines = content.Split(new[] {"\r\n", "\n"}, StringSplitOptions.None).ToList();
            var start = lines.FindIndex(l => l.Trim().Equals($"[{sectionName}]"));
            if (start == -1) return new List<string>();
            var res = new List<string>();
            for (int i = start + 1; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.StartsWith('[')) break;
                if (string.IsNullOrWhiteSpace(line)) continue;
                res.Add(line.Trim());
            }
            return res;
        }
        // Ensure general section matches exactly
        var origGeneral = GetSectionLines(beatmapString, "General");
        var encGeneral = GetSectionLines(encodedBeatmap, "General");
        Assert.Equal(origGeneral, encGeneral);
        
        // Ensure editor section matches exactly
        var origEditor = GetSectionLines(beatmapString, "Editor");
        var encEditor = GetSectionLines(encodedBeatmap, "Editor");
        
        // Ensure colours section matches exactly
        var origColours = GetSectionLines(beatmapString, "Colours");
        var encColours = GetSectionLines(encodedBeatmap, "Colours");
        Assert.Equal(origColours, encColours);

        // Ensure metadata section matches exactly
        var origMeta = GetSectionLines(beatmapString, "Metadata");
        var encMeta = GetSectionLines(encodedBeatmap, "Metadata");
        Assert.Equal(origMeta, encMeta);
        
        // Ensure timing section matches exactly
        var origTiming = GetSectionLines(beatmapString, "TimingPoints");
        var encTiming = GetSectionLines(encodedBeatmap, "TimingPoints");

        static List<string> NormalizeTiming(List<string> lines) => lines.Select(l =>
        {
            var idx = l.IndexOf(',');
            return idx >= 0 ? l.Substring(idx + 1) : l;
        }).ToList();

        Assert.Equal(NormalizeTiming(origTiming), NormalizeTiming(encTiming));
        
        var origHits = GetSectionLines(beatmapString, "HitObjects");
        var encHits = GetSectionLines(encodedBeatmap, "HitObjects");

        static List<string> NormalizeHitObjects(List<string> lines) => lines.Select(l =>
        {
            var parts = l.Split(',');
            if (parts.Length <= 3) return l.Trim();
            var list = parts.ToList();
            list.RemoveAt(2); // remove the time field
            return string.Join(",", list).Trim();
        }).ToList();

        Assert.Equal(NormalizeHitObjects(origHits), NormalizeHitObjects(encHits));
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
        static List<string> GetSectionLines(string content, string sectionName)
        {
            var lines = content.Split(new[] {"\r\n", "\n"}, StringSplitOptions.None).ToList();
            var start = lines.FindIndex(l => l.Trim().Equals($"[{sectionName}]"));
            if (start == -1) return new List<string>();
            var res = new List<string>();
            for (int i = start + 1; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line.StartsWith('[')) break;
                if (string.IsNullOrWhiteSpace(line)) continue;
                res.Add(line.Trim());
            }
            return res;
        }
        // Ensure general section matches exactly
        var origGeneral = GetSectionLines(beatmapString, "General");
        var encGeneral = GetSectionLines(encodedBeatmap, "General");
        Assert.Equal(origGeneral, encGeneral);
        
        // Ensure editor section matches exactly
        var origEditor = GetSectionLines(beatmapString, "Editor");
        var encEditor = GetSectionLines(encodedBeatmap, "Editor");
        
        // Ensure colours section matches exactly
        var origColours = GetSectionLines(beatmapString, "Colours");
        var encColours = GetSectionLines(encodedBeatmap, "Colours");
        Assert.Equal(origColours, encColours);

        // Ensure metadata section matches exactly
        var origMeta = GetSectionLines(beatmapString, "Metadata");
        var encMeta = GetSectionLines(encodedBeatmap, "Metadata");
        Assert.Equal(origMeta, encMeta);
        
        // Ensure timing section matches exactly
        var origTiming = GetSectionLines(beatmapString, "TimingPoints");
        var encTiming = GetSectionLines(encodedBeatmap, "TimingPoints");

        static List<string> NormalizeTiming(List<string> lines) => lines.Select(l =>
        {
            var idx = l.IndexOf(',');
            return idx >= 0 ? l.Substring(idx + 1) : l;
        }).ToList();

        Assert.Equal(NormalizeTiming(origTiming), NormalizeTiming(encTiming));
        
        var origHits = GetSectionLines(beatmapString, "HitObjects");
        var encHits = GetSectionLines(encodedBeatmap, "HitObjects");

        static List<string> NormalizeHitObjects(List<string> lines) => lines.Select(l =>
        {
            var parts = l.Split(',');
            if (parts.Length <= 3) return l.Trim();
            var list = parts.ToList();
            list.RemoveAt(2); // remove the time field
            return string.Join(",", list).Trim();
        }).ToList();

        Assert.Equal(NormalizeHitObjects(origHits), NormalizeHitObjects(encHits));
    }
}
