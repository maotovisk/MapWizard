using BeatmapParser;
using BeatmapParser.Enums;
using MapWizard.Tools.Results;
using MapWizard.Tools.HelperExtensions;
using MapWizard.Tools.HitSounds.Extensions;

namespace MapWizard.Tools.HitSounds.Copier;
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
    /// <returns>The target with the hit sounds applied, or a failure when the source can't be copied from.</returns>
    public static Result<Beatmap> CopyFromBeatmap(Beatmap source, Beatmap target, HitSoundCopierOptions options)
    {
        if (source.GeneralSection.Mode == Ruleset.Mania)
            return HitSoundErrors.ManiaNotSupported;

        if (source.TimingPoints == null) return target;
        
        // This is a timeline map of the sound events in the source beatmap,
        // For now this will generate basically the sounds to be applied. 
        // When a mania beatmap is the origin, we generate the "merged"
        // sound events. This will also generate the files needed for
        // sample "blending"
        var hitSoundTimeline = source.BuildTimeline();
        
        target.ApplyNonDraggableHitSounds(hitSoundTimeline: hitSoundTimeline.NonDraggableSoundTimeline, options);
        
        if (options.CopySliderBodySounds)
            target.ApplyDraggableHitSounds(hitSoundTimeline.DraggableSoundTimeline, options);

        target.ApplySampleTimeline(hitSoundTimeline.SampleSetTimeline, options);

        target.TimingPoints?.RemoveRedundantGreenLines();
        
        return target;
    }

    /// <summary>
    /// Copy hit sounds from one beatmap to multiple others.
    /// </summary>
    /// <param name="sourcePath"></param>
    /// <param name="targetPath"></param>
    /// <param name="options"></param>
    /// <returns>Success, or the first failure. On failure nothing has been written to the targets.</returns>
    public static Result CopyFromBeatmapToTarget(string sourcePath, string[] targetPath, HitSoundCopierOptions options)
    {
        foreach (var path in targetPath.Prepend(sourcePath))
        {
            if (!File.Exists(path)) return HitSoundErrors.FileNotFound(path);
        }

        var sourceFile = Beatmap.Decode(File.ReadAllText(sourcePath));
        foreach (var path in targetPath)
        {
            var targetFile = Beatmap.Decode(File.ReadAllText(path));
            var copy = CopyFromBeatmap(sourceFile, targetFile, options);
            if (copy.IsFailure) return copy;

            var output = copy.Value;
            HitSoundSampleSynchronizer.SynchronizeIfNeeded(sourceFile, sourcePath, output, path, options);

            if (!File.Exists(path)) continue;

            BeatmapBackupHelper.CreateBackupCopy(path);
                
            File.WriteAllText(path, output.Encode().Replace("\r\n", "\n").Replace("\n", "\r\n"));
        }

        return Result.Success();
    }
}
