using BeatmapParser;
using BeatmapParser.Enums;

namespace MapWizard.Tools.HitSounds.Extensions;

public static class BeatmapExtensions
{
    /// <summary>
    /// Maps the audio file names to samplesets based on hints from the hitobjects
    /// </summary>
    /// <param name="origin">the origin beatmap.</param>
    /// <returns></returns>
    public static Dictionary<string, (SampleSet sampleset, HitSound sound)> MapManiaSoundsToSamples(this Beatmap origin)
    {
        var sampleLookup = new Dictionary<string, (SampleSet sampleset, HitSound sound)>();

        foreach (var hitObject in origin.HitObjects.Objects)
        {
            
        }
        
        return sampleLookup;
    } 
    
}