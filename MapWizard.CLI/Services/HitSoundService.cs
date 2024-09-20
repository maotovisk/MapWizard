using MapWizard.Tools.HitSoundCopier;

namespace MapWizard.Services;

public class HitSoundService
{
    public void CopyHitsounds(string sourcePath, string targetPath)
    {
        HitSoundCopier.CopyFromBeatmapToTarget(sourcePath, new []{targetPath});
    }

    public void CopyHitsounds(string sourcePath, string targetPath, double offset)
    {
        throw new Exception("Not implemented");
    }

    public void CopyHitsounds(string sourcePath, string targetPath, double offset, double leniency)
    {
        throw new Exception("Not implemented");
    }

    public void CopyHitsounds(string sourcePath, string targetPath, double offset, double leniency,
        bool copySampleChanges)
    {
        throw new Exception("Not implemented");
    }
}