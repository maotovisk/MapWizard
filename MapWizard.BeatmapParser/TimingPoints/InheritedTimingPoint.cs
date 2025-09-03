using System.Globalization;
using MapWizard.BeatmapParser;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents an inherited timing point of the beatmap.
/// </summary>
public class InheritedTimingPoint : TimingPoint
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
    public InheritedTimingPoint(
        TimeSpan time,
        SampleSet sampleSet,
        uint sampleIndex,
        uint volume,
        List<Effect> effects,
        double sliderVelocity) : base(time, sampleSet, sampleIndex, volume, effects)
    {
        SliderVelocity = sliderVelocity;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InheritedTimingPoint"/> class.
    /// </summary>
    public InheritedTimingPoint()
    {
        SliderVelocity = 1.0;
    }

    /// <summary>
    /// Parses a timing point line into a new <see cref="TimingPoint"/> class.
    /// </summary>
    /// <returns></returns>
    public string Encode(TimingPoints section)
    {
        return $"{Helper.FormatTime(Time.TotalMilliseconds)},{Math.Round(-100.0 / SliderVelocity, 13).ToString(CultureInfo.InvariantCulture)},{section.GetUninheritedTimingPointAt(Time.TotalMilliseconds)?.TimeSignature ?? 4},{(int)SampleSet},{SampleIndex},{Volume},{0},{Helper.EncodeEffects(Effects)}";
    }
}