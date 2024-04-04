namespace BeatmapParser;

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
    /// Kiai time effect.
    /// </summary>
    Kiai = 1,

    /// <summary>
    /// Omit first bar line effect.
    /// </summary>
    OmitFirstBarLine = 8
}