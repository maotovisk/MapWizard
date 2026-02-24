using BeatmapParser.Enums;

namespace MapWizard.Desktop.Models.HitSoundVisualizer;

public class HitSoundVisualizerPoint
{
    public int Id { get; set; }
    public int TimeMs { get; set; }
    public SampleSet SampleSet { get; set; } = SampleSet.Normal;
    public HitSound HitSound { get; set; } = HitSound.Normal;
    public bool IsDraggable { get; set; }
}
