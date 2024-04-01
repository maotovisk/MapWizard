namespace BeatmapParser;

/// <summary>
/// Represents an timing point of the beatmap.
/// </summary>
public interface ITimingPoint
{
    /// <summary>
    /// Gets or sets the start time timing point of the beatmap.
    /// </summary>
    TimeSpan Time { get; set; }

    /// <summary>
    /// Gets or sets the sample set of objects in the beatmap.
    /// </summary>
    SampleSet SampleSet { get; set; }

    /// <summary>
    /// Gets or sets the custom sample index of objects in the beatmap.
    /// </summary>
    uint SampleIndex { get; set; }

    /// <summary>
    /// Gets or sets the volume of objects in the beatmap.
    /// </summary>
    uint Volume { get; set; }

    /// <summary>
    /// Gets or sets the effects of objects in the beatmap.
    /// </summary>
    List<Effect> Effects { get; set; }
}