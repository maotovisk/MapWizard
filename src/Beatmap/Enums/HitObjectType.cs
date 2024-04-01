
using osu;

/// <summary>
/// Represents the type of a hit object in a beatmap.
/// </summary>
public enum HitObjectType : int
{
    /// <summary>
    /// Represents a circle object.
    /// </summary>
    Circle = 0x000000F,

    /// <summary>
    /// Represents a slider object.
    /// </summary>
    Slider = 0x00000F0,

    /// <summary>
    /// Represents a spinner object.
    /// </summary>
    Spinner = 0x000F000,

    /// <summary>
    /// Represents a mania hold object.
    /// </summary>
    ManiaHold = 0x0F00000
}