namespace Beatmap;

/// <summary>
/// Represents an uninherited timing point of the beatmap.
/// </summary>
public interface IUninheritedTimingPoint : ITimingPoint
{
    /// <summary>
    /// Gets or sets the duration of a beats of the beatmap.
    /// </summary>
    TimeSpan BeatLength { get; set; }

    /// <summary>
    /// Gets or sets Amount of beats in a measure of the beatmap.
    /// </summary>
    int TimeSignature { get; set; }
}