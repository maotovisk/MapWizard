using System.Globalization;

namespace BeatmapParser.Sections;

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
        TimingPointList = [];
    }

    /// <summary>
    /// Parses a list of TimingPoints lines into a new <see cref="TimingPoints"/> class.
    /// </summary>
    /// <param name="section"></param>
    /// <returns></returns>
    public static TimingPoints Decode(List<string> section)
    {

        // Timing point structure:
        // time beatLength  meter   sampleSet   sampleIndex     volume  uninherited     effects
        // 0    1           2       3           4               5       6               7

        List<ITimingPoint> timingPoints = [];
        try
        {
            foreach (var line in section)
            {
                var split = line.Split(',');
                if (split[6] == "1")
                {
                    timingPoints.Add(new UninheritedTimingPoint(
                        time: TimeSpan.FromMilliseconds(double.Parse(split[0], CultureInfo.InvariantCulture)),
                        sampleSet: (SampleSet)Enum.Parse(typeof(SampleSet), split[3]),
                        sampleIndex: uint.Parse(split[4]),
                        volume: uint.Parse(split[5]),
                        effects: Helper.ParseEffects(int.Parse(split[6])),
                        beatLength: TimeSpan.FromMilliseconds(double.Parse(split[1], CultureInfo.InvariantCulture)),
                        timeSignature: int.Parse(split[2])
                    ));
                }
                else
                {
                    timingPoints.Add(new InheritedTimingPoint(
                        time: TimeSpan.FromMilliseconds(double.Parse(split[0])),
                        sampleSet: (SampleSet)Enum.Parse(typeof(SampleSet), split[3]),
                        sampleIndex: uint.Parse(split[4]),
                        volume: uint.Parse(split[5]),
                        effects: Helper.ParseEffects(int.Parse(split[6], CultureInfo.InvariantCulture)),
                        sliderVelocity: -(100 / double.Parse(split[1], CultureInfo.InvariantCulture))
                    ));
                }
            };

            return new TimingPoints(timingPoints);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse TimingPoints: {ex.Message}\n{ex.StackTrace}");
        }
    }
}