namespace MapWizard.BeatmapParser;
/// <summary>
/// Represents the type of a hit object in a beatmap.
/// </summary>
public enum HitObjectType : int
{
    /// <summary>
    /// Represents a circle hit object.
    /// </summary>
    Circle = 1 << 0,

    /// <summary>
    /// Represents a slider hit object.
    /// </summary>
    Slider = 1 << 1,

    /// <summary>
    /// Represents a spinner hit object.
    /// </summary>
    Spinner = 1 << 3,

    /// <summary>
    /// Represents a mania hold hit object.
    /// </summary>
    ManiaHold = 1 << 7
}
