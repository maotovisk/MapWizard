using Beatmap.Events;

namespace Beatmap.Sections;

/// <summary>
///
/// </summary>
public interface IEvents
{
    /// <summary>
    /// Represents the list of events in the beatmap.
    /// </summary>
    List<IEvent> Events { get; set; }
}