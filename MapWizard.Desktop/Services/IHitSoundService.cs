using MapWizard.Tools.HitSounds.Copier;

namespace MapWizard.Desktop.Services;

public interface IHitSoundService
{
    public HitSoundTimingCompatibilityReport AnalyzeTimingCompatibility(string sourcePath, string[] targetPaths);
    public bool CopyHitsoundsAsync(string sourcePath, string[] targetPath, HitSoundCopierOptions options);
}
