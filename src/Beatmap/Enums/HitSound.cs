namespace Beatmap;

/// <summary>
/// Represents the hit sounds that a hit object can have.
/// </summary>
public enum HitSound : int
{
    /// <summary>
    /// Normal hit sound.
    /// </summary>
    Normal = 0x0000000F,

    /// <summary>
    /// Whistle hit sound.
    /// </summary>
    Whistle = 0x000000F0,

    /// <summary>
    /// Finish hit sound.
    /// </summary>
    Finish = 0x00000F00,

    /// <summary>
    /// Clap hit sound.
    /// </summary>
    Clap = 0x0000F000,
}