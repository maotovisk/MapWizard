namespace MapWizard.BeatmapParser;

/// <summary>
/// Represents the type of an event in a beatmap. Each event type corresponds to a specific type of element or action within the beatmap.
/// </summary>
public enum EventType : int
{
    /// <summary>
    /// Background event.
    /// </summary>
    Background = 0,

    /// <summary>
    /// Video event.
    /// </summary>
    Video = 1,

    /// <summary>
    /// Break event.
    /// </summary>
    Break = 2,

    /// <summary>
    /// Colour event.
    /// </summary>
    Colour = 3,

    /// <summary>
    /// Sprite event.
    /// </summary>
    Sprite = 4,

    /// <summary>
    /// Sample event.
    /// </summary>
    Sample = 5,

    /// <summary>
    /// Animation event.
    /// </summary>
    Animation = 6
}