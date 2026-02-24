using BeatmapParser.Enums;

namespace MapWizard.Desktop.Models.HitSoundVisualizer;

public class HitSoundVisualizerSampleChange
{
    public int TimeMs { get; set; }
    public SampleSet SampleSet { get; set; } = SampleSet.Normal;
    public int Index { get; set; }
    public int Volume { get; set; }
}
