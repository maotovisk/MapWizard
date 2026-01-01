using MapWizard.Tools.HitSounds.Copier;

namespace MapWizard.CLI.Services;

public class HitSoundService
{
    public void CopyHitsounds(string sourcePath, string targetPath, HitSoundCopierOptions options)
    {
        HitSoundCopier.CopyFromBeatmapToTarget(sourcePath, new []{targetPath}, options);
    }
}