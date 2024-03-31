/// <summary>
/// Represents the timing points section section of a beatmap.
/// </summary>
public interface ITimingPointsSection
{
    /// <summary>
    /// Gets or sets the timing points of the beatmap.
    /// </summary>
    List<ITimingPoint> TimingPoints { get; set; }
}