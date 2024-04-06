namespace MapWizard.BeatmapParser;

/// <summary>
///
/// </summary>
public interface ISpinner : IHitObject
{
    /// <summary>
    /// 
    /// </summary>
    public TimeSpan End { get; set; }
}