namespace MapWizard.Desktop.Models.HitSoundVisualizer;

public class HitSoundTimelineContextRequest
{
    public int TimeMs { get; set; }
    public int PointId { get; set; } = -1;
    public int? SampleChangeTimeMs { get; set; }
    public bool IsSampleRow { get; set; }
}
