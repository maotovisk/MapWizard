using System.Numerics;
using System.Reflection.Metadata.Ecma335;

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


    public TimingPoints FromData(ref Beatmap beatmap, List<string> section)
    {
        List<TimingPoint> timingPoints = section.Select(x =>
        {
            var split = x.Split(',');

            return new TimingPoint()
            {
                Time = TimeSpan.FromMilliseconds(double.Parse(split[0])),
                SampleSet = (SampleSet)int.Parse(split[1]),
                SampleIndex = uint.Parse(split[2]),
                Volume = uint.Parse(split[3]),
                Effects = new List<Effect> { (Effect)int.Parse(split[4]) }
            };
        }).ToList();

        return new TimingPoints(timingPoints);
    }


}