using System.Collections.Generic;
using MapWizard.Desktop.Models.HitSoundVisualizer;
using MapWizard.Tools.HitSounds.Timeline;
using MapWizard.Tools.HitSounds.Copier;

namespace MapWizard.Desktop.Services.HitsoundService;

public interface IHitSoundService
{
    public HitSoundTimingCompatibilityReport AnalyzeTimingCompatibility(string sourcePath, string[] targetPaths);
    public bool CopyHitsounds(string sourcePath, string[] targetPaths, HitSoundCopierOptions options);
    public HitSoundVisualizerDocument LoadHitsoundVisualizerDocument(string beatmapPath);
    public IReadOnlyList<HitSoundVisualizerSnapTick> BuildHitsoundVisualizerSnapTicks(string beatmapPath, double endTimeMs);
    public bool ApplyVisualizerTimelineToBeatmap(string targetBeatmapPath, HitSoundTimeline timeline, out string errorMessage);
    public bool ExportVisualizerHitsoundDiff(string sourceBeatmapPath, HitSoundTimeline timeline, out string exportedBeatmapPath, out string errorMessage);
}
