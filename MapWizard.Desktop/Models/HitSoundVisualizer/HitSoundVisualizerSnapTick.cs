namespace MapWizard.Desktop.Models.HitSoundVisualizer;

public class HitSoundVisualizerSnapTick
{
    public int TimeMs { get; set; }
    public double ExactTimeMs { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Strength { get; set; }
    public int Denominator { get; set; }
    public bool IsMeasureLine { get; set; }
}
