namespace Beatmap.Sections;

/// <summary>
///
/// </summary>
public interface IEvents : IHitObject
{
    /// <summary>
    /// Represents the list of events in the beatmap.
    /// </summary>
    List<IEvent> Events { get; set; }
}