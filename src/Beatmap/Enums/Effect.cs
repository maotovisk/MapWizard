namespace Beatmap.Enums;

/// <summary>
/// Represents an effect in a control point.
/// </summary>
public enum Effect
{
    /// <summary>
    /// No effect.
    /// </summary>
    None = 0,
    /// <summary>
    /// Kiai effect.
    /// </summary>
    Kiai = 1,
    /// <summary>
    /// Omit first bar line effect, used in osu!mania.
    /// </summary>
    OmitFirstBarLine = 2,
}