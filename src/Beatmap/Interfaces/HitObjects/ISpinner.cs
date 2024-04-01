namespace BeatmapParser;

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