using System.Numerics;

namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents a control point of a slider path, optionally starting a new segment with a specific curve type.
/// </summary>
public struct CurvePoint
{
    /// <summary>
    /// Optional curve type marker for this control point (only set at segment starts in v128).
    /// </summary>
    public CurveType? Type { get; set; }

    /// <summary>
    /// Optional degree for BSpline segments (ignored for other types).
    /// </summary>
    public int? Degree { get; set; }

    /// <summary>
    /// Absolute position of the control point.
    /// </summary>
    public Vector2 Position { get; set; }

    public CurvePoint(Vector2 position, CurveType? type = null, int? degree = null)
    {
        Position = position;
        Type = type;
        Degree = degree;
    }
}

