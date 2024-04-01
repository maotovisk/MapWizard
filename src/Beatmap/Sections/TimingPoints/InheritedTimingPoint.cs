namespace BeatmapParser;

/// <summary>
/// Represents an inherited timing point of the beatmap.
/// </summary>
public class InheritedTimingPoint : TimingPoint, IInheritedTimingPoint
{
    /// <summary>
    /// Gets or sets the slider velocity of the beatmap.
    /// </summary>
    public double SliderVelocity { get; set; }

    ///  <summary>
    ///  Initializes a new instance of the <see cref="InheritedTimingPoint"/> class.
    /// </summary>
    /// <param name="time"></param>
    /// <param name="sampleSet"></param>
    /// <param name="sampleIndex"></param>
    /// <param name="volume"></param>
    /// <param name="effects"></param>
    /// <param name="sliderVelocity"></param>
    public InheritedTimingPoint(TimeSpan time, SampleSet sampleSet, uint sampleIndex, uint volume, List<Effect> effects, double sliderVelocity) : base(time, sampleSet, sampleIndex, volume, effects)
    {
        SliderVelocity = sliderVelocity;
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="InheritedTimingPoint"/> class.
    /// </summary>
    public InheritedTimingPoint() : base()
    {
        Time = TimeSpan.Zero;
        SampleSet = SampleSet.Default;
        SampleIndex = 0;
        Volume = 0;
        Effects = [];
        SliderVelocity = 1.0;
    }
}