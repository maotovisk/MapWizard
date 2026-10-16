using MapWizard.Tools.HitSounds.Copier;
using MapWizard.Tools.Results;

namespace MapWizard.CLI.Services;

public class HitSoundService
{
    public Result CopyHitsounds(string sourcePath, string targetPath, HitSoundCopierOptions options)
    {
        return HitSoundCopier.CopyFromBeatmapToTarget(sourcePath, new []{targetPath}, options);
    }
}