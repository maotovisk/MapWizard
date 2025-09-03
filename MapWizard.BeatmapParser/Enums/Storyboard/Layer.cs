namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents the different layers that can be used to organize or render elements
/// in a beatmap or visual environment. These layers dictate the rendering order
/// or logical grouping of visual and sound elements.
/// </summary>
public enum Layer : int
{
    /// <summary>
    /// Represents the background layer of a sprite or other visual element.
    /// This layer is typically rendered behind all other layers.
    /// </summary>
    Background = 0,

    /// <summary>
    /// Represents the layer where fail-specific visual effects or elements are rendered.
    /// Typically used to display graphics or animations that appear when a failure condition is met.
    /// </summary>
    Fail = 1,

    /// <summary>
    /// Represents the layer for events that occur when a player successfully passes a level.
    /// This layer is typically associated with effects or elements displayed at the end of gameplay upon success.
    /// </summary>
    Pass = 2,

    /// <summary>
    /// Represents the foreground layer of a sprite or other visual element.
    /// This layer is typically rendered above most other layers for primary focus or emphasis.
    /// </summary>
    Foreground = 3,

    /// <summary>
    /// Represents a video layer in a sprite or visual element.
    /// This layer is typically used to render video content.
    /// </summary>
    Video = 4,

    Overlay = int.MinValue
}