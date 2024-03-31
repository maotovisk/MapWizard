namespace Beatmap;

/// <summary>
/// Represents an timing point of the beatmap.
/// </summary>
public class TimingPoint : ITimingPoint
{
    /// <summary>
    /// Gets or sets the start time timing point of the beatmap.
    /// </summary>
    public TimeSpan Time { get; set; }

    /// <summary>
    /// Gets or sets the sample set of objects in the beatmap.
    /// </summary>
    public SampleSet SampleSet { get; set; }

    /// <summary>
    /// Gets or sets the custom sample index of objects in the beatmap.
    /// </summary>
    public uint SampleIndex { get; set; }

    /// <summary>
    /// Gets or sets the volume of objects in the beatmap.
    /// </summary>
    public uint Volume { get; set; }

    /// <summary>
    /// Gets or sets the effects of objects in the beatmap.
    /// </summary>
    public List<Effect> Effects { get; set; }

    ///  <summary>
    ///  Initializes a new instance of the <see cref="TimingPoint"/> class.
    /// </summary>
    /// <param name="time"></param>
    /// <param name="sampleSet"></param>
    /// <param name="sampleIndex"></param>
    /// <param name="volume"></param>
    /// <param name="effects"></param>
    public TimingPoint(TimeSpan time, SampleSet sampleSet, uint sampleIndex, uint volume, List<Effect> effects)
    {
        Time = time;
        SampleSet = sampleSet;
        SampleIndex = sampleIndex;
        Volume = volume;
        Effects = effects;
    }

    public TimingPoint()
    {
        Time = TimeSpan.Zero;
        SampleSet = SampleSet.Default;
        SampleIndex = 0;
        Volume = 0;
        Effects = new List<Effect>();
    }


    public TimingPoint FromData(ref Beatmap beatmap, List<string> section)
    {
        beatmap.TimingPoints = section.Select(x =>
        {
            var split = x.Split(',');
            return new TimingPoint
            {
                Offset = double.Parse(split[0]),
                MillisecondsPerBeat = double.Parse(split[1]),
                Meter = int.Parse(split[2]),
                SampleSet = int.Parse(split[3]),
                SampleIndex = int.Parse(split[4]),
                Volume = int.Parse(split[5]),
                Inherited = bool.Parse(split[6]),
                KiaiMode = bool.Parse(split[7])
            };
        }).ToList();
    }
}