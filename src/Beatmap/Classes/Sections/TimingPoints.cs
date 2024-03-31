using System.Numerics;

namespace Beatmap;

/// <summary>
/// Represents the timing points section of a <see cref="Beatmap"/>.
/// </summary>
public class TimingPoints : ITimingPoints
{
    /// <summary>
    /// Gets or sets the timing points of the beatmap.
    /// </summary>
    public List<ITimingPoint> TimingPointList { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TimingPoints"/> class.
    /// </summary>
    /// <param name="timingPointList"></param>
    public TimingPoints(List<ITimingPoint> timingPointList)
    {
        TimingPointList = timingPointList;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TimingPoints"/> class.
    /// </summary>
    public TimingPoints()
    {
        TimingPointList = new List<ITimingPoint>();
    }
}