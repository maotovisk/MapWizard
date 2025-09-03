namespace MapWizard.BeatmapParser;

/// <summary>
/// Specifies the parameter types that can be used in a map parsing context.
/// </summary>
public enum ParameterName : int
{
    /// <summary>
    /// Represents the horizontal flipping parameter for map parsing.
    /// Used to flip elements horizontally during the map parsing.
    /// </summary>
    FlipHorizontal = 'H',

    /// <summary>
    /// Represents the vertical flipping parameter for map parsing.
    /// Used to flip elements vertically during the map parsing process.
    /// </summary>
    FlipVertical = 'V',

    /// <summary>
    /// Represents the additive blending parameter for map parsing.
    /// Used to enable additive blending for visual elements during the map rendering process.
    /// </summary>
    AdditiveBlending = 'A',
}