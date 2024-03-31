using Beatmap.TimingPoints;

namespace Beatmap.Sections;

/// <summary>
/// Represents the timing points section section of a beatmap.
/// </summary>
public interface ITimingPoints
{
    /// <summary>
    /// Gets or sets the timing points of the beatmap.
    /// </summary>
    List<ITimingPoint> TimingPoints { get; set; }
}