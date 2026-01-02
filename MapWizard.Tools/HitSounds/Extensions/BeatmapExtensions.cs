using System.Reflection.Metadata.Ecma335;
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
    public static Dictionary<string, (SampleSet sampleSet, HitSound sound, int index)> MapManiaSoundsToSamples(this Beatmap origin)
    {
        var sampleLookup = new Dictionary<string, (SampleSet sampleset, HitSound sound, int index)>();
        var indexCounter = new Dictionary<(SampleSet, HitSound), int>();

        foreach (var hitObject in origin.HitObjects.Objects)
        {
            var fileName = hitObject.HitSounds.SampleData.FileName;
    
            if (string.IsNullOrEmpty(fileName) || sampleLookup.ContainsKey(fileName))
                continue;
            
            var sampleSet = hitObject.HitSounds.SampleData.NormalSet;
            
            // ps: we are assuming the mania hint is coming from the first defined hitsound only,
            // for now this is the only workflow supported.
            var sound = hitObject.HitSounds.Sounds[0];
            var key = (sampleSet, sound);

            indexCounter.TryGetValue(key, out var currentIndex);
            indexCounter[key] = ++currentIndex;
            
            sampleLookup.Add(fileName, (sampleSet, sound, currentIndex));
        }
        
        return sampleLookup;
    }
}