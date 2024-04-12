using System.Globalization;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents an uninherited timing point of the beatmap.
/// </summary>
public class UninheritedTimingPoint : TimingPoint
{
    /// <summary>
    /// Gets or sets the duration of a beats of the beatmap.
    /// </summary>
    public double BeatLength { get; set; }

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
    public UninheritedTimingPoint(
        TimeSpan time,
        SampleSet sampleSet,
        uint sampleIndex,
        uint volume,
        List<Effect> effects,
        double beatLength,
        int timeSignature) : base(time, sampleSet, sampleIndex, volume, effects)
    {
        BeatLength = beatLength;
        TimeSignature = timeSignature;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UninheritedTimingPoint"/> class.
    /// </summary>
    public UninheritedTimingPoint()
    {
        BeatLength = 500;
        TimeSignature = 4;
    }

    /// <summary>
    /// Parses a timing point line into a new <see cref="TimingPoint"/> class.
    /// </summary>
    /// <returns></returns>
    public string Encode()
    {
        return $"{Time.TotalMilliseconds.ToString(CultureInfo.InvariantCulture)},{BeatLength.ToString("G", CultureInfo.InvariantCulture)},{TimeSignature},{(int)SampleSet},{SampleIndex},{Volume},{1},{Helper.EncodeEffects(Effects)}";
    }

    /// <summary>
    /// Gets the BPM of the timing point.
    /// </summary>
    /// <returns></returns>
    public double GetBpm()
    {
        return 60000 / BeatLength;
    }
}