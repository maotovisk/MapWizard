namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents the types of sections in a beatmap file.
/// </summary>
public enum SectionType
{
    /// <summary>
    /// Represents the general section.
    /// </summary>
    General,

    /// <summary>
    /// Represents the editor section.
    /// </summary>
    Editor,

    /// <summary>
    /// Represents the metadata section.
    /// </summary>
    Metadata,

    /// <summary>
    /// Represents the difficulty section.
    /// </summary>
    Difficulty,

    /// <summary>
    /// Represents the colours section.
    /// </summary>
    Colours,

    /// <summary>
    /// Represents the events section.
    /// </summary>
    Events,

    /// <summary>
    /// Represents the timing points section.
    /// </summary>
    TimingPoints,

    /// <summary>
    /// Represents the hit objects section.
    /// </summary>
    HitObjects,
}
