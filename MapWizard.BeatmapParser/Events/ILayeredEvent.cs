namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents a contract for events that are associated with a specific storyboard layer.
/// </summary>
public interface ILayeredEvent
{
    /// <summary>
    /// Represents the layer on which an event or element is rendered in the storyboard.
    /// </summary>
    /// <remarks>
    /// The layer determines the draw order of elements in the storyboard. Lower layers are rendered
    /// behind higher layers. The available layers are:
    /// - Background: Used for elements that appear behind all other layers.
    /// - Fail: Used for elements shown during failed gameplay scenarios.
    /// - Pass: Used for elements shown during successful gameplay scenarios.
    /// - Foreground: Used for elements that appear in front of the Background, Fail, and Pass layers but behind the Video layer.
    /// - Video: Used for video elements that appear above Foreground and below Overlay.
    /// - Overlay: The topmost layer, used for elements or effects drawn above all others.
    /// </remarks>
    public Layer Layer { get; set; }
}