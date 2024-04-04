using System.Globalization;
using System.Text;

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

                var time = double.Parse(split[0], NumberStyles.Float, CultureInfo.InvariantCulture);
                var beatLength = double.Parse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture);
                var timeSignature = split.Length >= 3 ? int.Parse(split[2]) : 4;
                var sampleSet = split.Length >= 4 ? (SampleSet)Enum.Parse(typeof(SampleSet), split[3]) : SampleSet.Normal;
                var sampleIndex = split.Length >= 5 ? uint.Parse(split[4]) : 0;
                var volume = split.Length >= 6 ? uint.Parse(split[5]) : 100;
                var timingChange = split.Length >= 8 && int.Parse(split[6]) == 1;
                var effects = split.Length >= 7 ? Helper.ParseEffects(int.Parse(split[7])) : [];

                ITimingPoint timingPoint;

                if (timingChange)
                {
                    timingPoint = new UninheritedTimingPoint(
                        time: Helper.ClampTimeSpan(time),
                        beatLength: beatLength,
                        timeSignature: timeSignature,
                        sampleSet: sampleSet,
                        sampleIndex: sampleIndex,
                        volume: volume,
                        effects: effects
                    );
                }
                else
                {
                    var sliderVelocity = beatLength < 0 ? 100 / -beatLength : 1;
                    timingPoint = new InheritedTimingPoint(
                        time: Helper.ClampTimeSpan(time),
                        sampleSet: sampleSet,
                        sampleIndex: sampleIndex,
                        volume: volume,
                        effects: effects,
                        sliderVelocity: sliderVelocity
                    );
                }

                timingPoints.Add(timingPoint);
            };

            return new TimingPoints(timingPoints);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse TimingPoints: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Encodes the <see cref="TimingPoints"/> class into a string.
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        StringBuilder builder = new();

        foreach (var timingPoint in TimingPointList)
        {
            if (timingPoint is UninheritedTimingPoint uninheritedTimingPoint)
            {
                builder.AppendLine(uninheritedTimingPoint.Encode());
            }
            else if (timingPoint is InheritedTimingPoint inheritedTimingPoint)
            {
                builder.AppendLine(inheritedTimingPoint.Encode(this));
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Gets the timing point at the specified time.
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public UninheritedTimingPoint? GetUninheritedTimingPointAt(double time)
    {
        return TimingPointList.OfType<UninheritedTimingPoint>().Last(x => time >= x.Time.TotalMilliseconds);
    }
}