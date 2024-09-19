using System;
using System.Threading.Tasks;
using MapWizard.Tools.HitSoundCopier;

namespace MapWizard.Desktop.Services;

public class HitSoundService : IHitSoundService
{
    public bool CopyHitsoundsAsync(string sourcePath, string[] targetPaths)
    {
        try
        {
            HitSoundCopier.CopyFromBeatmapToTarget(sourcePath: sourcePath, targetPath: targetPaths);
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
}