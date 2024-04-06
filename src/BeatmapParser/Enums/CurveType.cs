namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents the type of curve used in a beatmap.
/// </summary>
public enum CurveType
{
    /// <summary>
    /// Represents a Catmull curve.
    /// </summary>
    Catmull,

    /// <summary>
    /// Represents a Bezier curve.
    /// </summary>
    Bezier,

    /// <summary>
    /// Represents a linear curve.
    /// </summary>
    Linear,

    /// <summary>
    /// Represents a perfect curve.
    /// </summary>
    PerfectCurve
}