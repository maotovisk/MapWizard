using System.Globalization;
using System.Text;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents the timing points section of a <see cref="Beatmap"/>.
/// </summary>
public class TimingPoints
{
    /// <summary>
    /// Gets or sets the timing points of the beatmap.
    /// </summary>
    public List<TimingPoint> TimingPointList { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TimingPoints"/> class.
    /// </summary>
    /// <param name="timingPointList"></param>
    private TimingPoints(List<TimingPoint> timingPointList)
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
    /// <param name="section">The raw lines from the [TimingPoints] section of an osu file.</param>
    /// <returns>A populated <see cref="TimingPoints"/> instance containing the parsed timing point objects.</returns>
    public static TimingPoints Decode(List<string> section)
    {
        // Timing point structure:
        // time beatLength  meter   sampleSet   sampleIndex     volume  uninherited     effects
        // 0    1           2       3           4               5       6               7

        List<TimingPoint> timingPoints = [];
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
                var timingChange = split.Length >= 7 && int.Parse(split[6]) == 1 || split.Length < 7 && beatLength >= 0;
                var effects = split.Length >= 8 ? Helper.ParseEffects(int.Parse(split[7])) : [];

                TimingPoint timingPoint;

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
            }

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
    /// <returns>A newline-separated string where each line is an encoded timing point suitable for the [TimingPoints] section.</returns>
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
    /// Gets the uninherited timing point active at the specified time.
    /// </summary>
    /// <param name="time">The time in milliseconds.</param>
    /// <returns>The latest <see cref="UninheritedTimingPoint"/> whose timestamp is less than or equal to the provided time; otherwise null if none exist.</returns>
    public UninheritedTimingPoint? GetUninheritedTimingPointAt(double time)
    {
        if (TimingPointList.Count == 0) return null;

        var matchingTimingPoints = TimingPointList.OfType<UninheritedTimingPoint>().ToList().Where(x => x.Time.TotalMilliseconds <= time).ToList();
        return matchingTimingPoints.Count == 0 ? null : matchingTimingPoints.Last();
    }

    /// <summary>
    /// Gets the inherited timing point (green line) active at the specified time.
    /// </summary>
    /// <param name="time">The time in milliseconds.</param>
    /// <returns>
    /// The matching <see cref="InheritedTimingPoint"/> if one is active at the provided time and is valid relative to the last uninherited timing point; otherwise null.
    /// </returns>
    public InheritedTimingPoint? GetInheritedTimingPointAt(double time)
    {
        if (TimingPointList.Count == 0) return null;

        var matchingInheritedTimingPoints = TimingPointList
            .OfType<InheritedTimingPoint>()
            .Where(x => x.Time.TotalMilliseconds <= time)
            .ToList();

        var closestInheritedTimingPoint = matchingInheritedTimingPoints.LastOrDefault();

        var matchingUninheritedTimingPoints = TimingPointList
            .OfType<UninheritedTimingPoint>()
            .Where(x => x.Time.TotalMilliseconds <= time)
            .ToList();

        var closestUninheritedTimingPoint = matchingUninheritedTimingPoints.LastOrDefault();

        if (closestInheritedTimingPoint == null || closestUninheritedTimingPoint == null)
        {
            return closestInheritedTimingPoint;
        }

        return closestInheritedTimingPoint.Time >= closestUninheritedTimingPoint.Time ? closestInheritedTimingPoint : null;
    }

    /// <summary>
    /// Returns the volume at the specified time.
    /// </summary>
    /// <param name="time">Time in milliseconds.</param>
    /// <returns>The volume (0–100) defined by the most recent timing point at or before the provided time; defaults to 100 if no timing points exist.</returns>
    public uint GetVolumeAt(double time)
    {
        var timingPoint = TimingPointList.LastOrDefault(x => x.Time.TotalMilliseconds <= time);
        return timingPoint?.Volume ?? 100;
    }

    /// <summary>
    /// Returns the BPM at the specified time.
    /// Defaults to 120 BPM if no timing point is found.
    /// </summary>
    /// <param name="time">Time in milliseconds.</param>
    /// <returns>The BPM computed from the most recent uninherited timing point; returned value is 60000 / beatLength.</returns>
    public double GetBpmAt(double time)
    {
        var timingPoint = GetUninheritedTimingPointAt(time);
        return 60000 / (timingPoint?.BeatLength ?? 500);
    }

    /// <summary>
    /// Returns the slider velocity at the specified time.
    /// </summary>
    /// <param name="time">Time in milliseconds.</param>
    /// <returns>The slider velocity defined by the active inherited timing point, or 1 if none exist.</returns>
    public double GetSliderVelocityAt(double time)
    {
        var timingPoint = GetInheritedTimingPointAt(time);
        return timingPoint?.SliderVelocity ?? 1;
    }

    /// <summary>
    /// Gets the timing points at or before the specified time, ordered by inheritance and then by time.
    /// </summary>
    /// <param name="time">Time in milliseconds.</param>
    /// <returns>A list of matching <see cref="TimingPoint"/> objects. Returns an empty list if none match.</returns>
    public List<TimingPoint> GetTimingPointsAt(double time)
    {
        var matchingTimingPoints = TimingPointList.Where(x => x.Time.TotalMilliseconds <= time)
        .OrderByDescending(x => x is InheritedTimingPoint)
        .ThenBy(x => x.Time)
        .ToList();

        return matchingTimingPoints.Count == 0 ? [] : matchingTimingPoints;
    }
}