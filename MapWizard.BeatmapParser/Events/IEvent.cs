namespace MapWizard.BeatmapParser;

/// <summary>
/// Defines a common interface for all osu! beatmap events.
/// </summary>
public interface IEvent : IEncodable

{
    /// <summary>
    /// Represents the type of an event in the beatmap parser.
    /// </summary>
    /// <remarks>
    /// This property indicates the specific <see cref="EventType"/> of the event,
    /// allowing identification and categorization of various event types such as
    /// Background, Video, Break, Colour, Sprite, Sample, or Animation.
    /// </remarks>
    public EventType Type { get; init; }
}