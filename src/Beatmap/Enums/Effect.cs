namespace BeatmapParser;

/// <summary>
/// Represents an effect in a control point.
/// </summary>
public enum Effect
{
    /// <summary>
    /// No effect.
    /// </summary>
    None = 0x00000000,
    /// <summary>
    /// Kiai effect.
    /// </summary>
    Kiai = 0x0000000F,
    /// <summary>
    /// Omit first bar line effect, used in osu!mania.
    /// </summary>
    OmitFirstBarLine = 0x0000F000,
}