using System;
using MapWizard.Tools.HitSoundCopier;

namespace MapWizard.Desktop.Services;

public class HitSoundService : IHitSoundService
{
    public bool CopyHitsoundsAsync(string sourcePath, string[] targetPaths, HitSoundCopierOptions options)
    {
        try
        {
            HitSoundCopier.CopyFromBeatmapToTarget(sourcePath: sourcePath, targetPath: targetPaths, options: options);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return false;
        }
        
        return true;
    }
}