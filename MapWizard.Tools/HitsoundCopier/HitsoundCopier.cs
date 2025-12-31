using BeatmapParser;
using BeatmapParser.Enums;
using BeatmapParser.HitObjects;
using BeatmapParser.HitObjects.HitSounds;
using BeatmapParser.TimingPoints;
using MapWizard.Tools.Helpers;

namespace MapWizard.Tools.HitSoundCopier;
/// <summary>
/// Class to copy hit sounds from one beatmap to others.
/// </summary>
public static class HitSoundCopier
{
    /// <summary>
    /// Copy hit sounds from one beatmap to another.
    /// </summary>
    /// <param name="source">Source `Beatmap` object.</param>
    /// <param name="target">Target `Beatmap` object.</param>
    /// <param name="options">Options for the hit sound copying.</param>
    public static Beatmap CopyFromBeatmap(Beatmap source, Beatmap target, HitSoundCopierOptions options)
    {
        if (source.TimingPoints == null) return target;
        
        // This is a "Timeline" of the sound events in the source beatmap,
        // For now this will generate basically the sounds to be 
        // applied. When a mania beatmap is the origin, we
        // generate the "merged" sound event.
        var (hitSoundTimeLine, sliderBodyTimeline) = source.BuildHitSoundTimelines();
        
        target.ApplyNonDraggableHitSounds(hitSoundTimeline: hitSoundTimeLine, options);
        
        if (options.CopySliderBodySounds)
            target.ApplyDraggableHitSounds(sliderBodyTimeline, options);
        
        // This is basically the same thing as above, but for the sample sets.
        // When using a mania beatmap as the origin, we take the merged info
        // so we can generate the merged sample sets to be used with the
        // merged sounds.
        var sampleSetTimeline = source.BuildSampleSetTimeline();

        target.ApplySampleTimeline(sampleSetTimeline, options);

        target.TimingPoints?.TimingPointList = TimingPointHelper.RemoveRedundantGreenLines(target.TimingPoints);
        
        return target;
    }

    /// <summary>
    /// Copy hit sounds from one beatmap to multiple others.
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="targetPath"></param>
    /// <param name="options"></param>
    public static void CopyFromBeatmapToTarget(string sourcePath, string[] targetPath, HitSoundCopierOptions options)
    {
        var sourceFile = Beatmap.Decode(File.ReadAllText(sourcePath));
        foreach (var path in targetPath)
        {
            var targetFile = Beatmap.Decode(File.ReadAllText(path));
            var output = CopyFromBeatmap(sourceFile, targetFile, options);

            if (!File.Exists(sourcePath)) continue;
            
            if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)))
            {
                var backupDirectory = Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/MapWizard/Backup");
                    
                var currentTimestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
                File.Move(path, backupDirectory.FullName + "/" + currentTimestamp + Path.GetFileName(path));
            }
                
            File.WriteAllText(path, output.Encode().Replace("\r\n", "\n").Replace("\n", "\r\n"));
        }
    }
}
