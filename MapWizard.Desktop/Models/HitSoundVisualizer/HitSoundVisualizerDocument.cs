using System.Collections.Generic;
using MapWizard.Tools.HitSounds.Timeline;

namespace MapWizard.Desktop.Models.HitSoundVisualizer;

public class HitSoundVisualizerDocument
{
    public string BeatmapPath { get; set; } = string.Empty;
    public string MapsetDirectoryPath { get; set; } = string.Empty;
    public string AudioFilePath { get; set; } = string.Empty;
    public string DisplayTitle { get; set; } = string.Empty;
    public double EndTimeMs { get; set; }

    // Export-compatible working structure used by the hitsound copier.
    public HitSoundTimeline Timeline { get; set; } = new();

    public IReadOnlyList<HitSoundVisualizerPoint> Points { get; set; } = [];
    public IReadOnlyList<HitSoundVisualizerSampleChange> SampleChanges { get; set; } = [];
    public IReadOnlyList<HitSoundVisualizerSnapTick> SnapTicks { get; set; } = [];
}
