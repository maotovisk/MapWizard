using System.Collections.Generic;
using MapWizard.Desktop.Models.HitSoundVisualizer;
using MapWizard.Tools.HitSounds.Copier;

namespace MapWizard.Desktop.Services.HitsoundService;

public interface IHitSoundService
{
    public HitSoundTimingCompatibilityReport AnalyzeTimingCompatibility(string sourcePath, string[] targetPaths);
    public bool CopyHitsoundsAsync(string sourcePath, string[] targetPath, HitSoundCopierOptions options);
    public HitSoundVisualizerDocument LoadHitsoundVisualizerDocument(string beatmapPath);
    public IReadOnlyList<HitSoundVisualizerSnapTick> BuildHitsoundVisualizerSnapTicks(string beatmapPath, double endTimeMs);
}
