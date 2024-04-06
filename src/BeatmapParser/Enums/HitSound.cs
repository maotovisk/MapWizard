namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents the hit sounds that a hit object can have.
/// </summary>
public enum HitSound : int
{
    /// <summary>
    /// No hit sound.
    /// </summary>
    None = 0,

    /// <summary>
    /// Normal hit sound.
    /// </summary>
    Normal = 1,

    /// <summary>
    /// Whistle hit sound.
    /// </summary>
    Whistle = 2,

    /// <summary>
    /// Finish hit sound.
    /// </summary>
    Finish = 4,

    /// <summary>
    /// Clap hit sound.
    /// </summary>
    Clap = 8
}