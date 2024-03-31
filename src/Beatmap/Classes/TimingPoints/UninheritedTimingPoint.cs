namespace Beatmap;

/// <summary>
/// Represents an uninherited timing point of the beatmap.
/// </summary>
public class UninheritedTimingPoint : TimingPoint, IUninheritedTimingPoint
{
    /// <summary>
    /// Gets or sets the duration of a beats of the beatmap.
    /// </summary>
    public TimeSpan BeatLength { get; set; }

    /// <summary>
    /// Gets or sets Amount of beats in a measure of the beatmap.
    /// </summary>
    public int TimeSignature { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UninheritedTimingPoint"/> class.
    /// </summary>
    /// <param name="time"></param>
    /// <param name="sampleSet"></param>
    /// <param name="sampleIndex"></param>
    /// <param name="volume"></param>
    /// <param name="effects"></param>
    /// <param name="beatLength"></param>
    /// <param name="timeSignature"></param>
    public UninheritedTimingPoint(TimeSpan time, SampleSet sampleSet, uint sampleIndex, uint volume, List<Effect> effects, TimeSpan beatLength, int timeSignature) : base(time, sampleSet, sampleIndex, volume, effects)
    {
        BeatLength = beatLength;
        TimeSignature = timeSignature;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UninheritedTimingPoint"/> class.
    /// </summary>
    public UninheritedTimingPoint() : base()
    {
        Time = TimeSpan.Zero;
        SampleSet = SampleSet.Default;
        SampleIndex = 0;
        Volume = 0;
        Effects = new List<Effect>();
        BeatLength = TimeSpan.Zero;
        TimeSignature = 4;
    }
}