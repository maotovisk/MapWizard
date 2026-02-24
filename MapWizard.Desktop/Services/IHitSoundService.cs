using MapWizard.Tools.HitSounds.Copier;
using MapWizard.Desktop.Models.HitSoundVisualizer;

namespace MapWizard.Desktop.Services;

public interface IHitSoundService
{
    public HitSoundTimingCompatibilityReport AnalyzeTimingCompatibility(string sourcePath, string[] targetPaths);
    public bool CopyHitsoundsAsync(string sourcePath, string[] targetPath, HitSoundCopierOptions options);
    public HitSoundVisualizerDocument LoadHitsoundVisualizerDocument(string beatmapPath);
}
