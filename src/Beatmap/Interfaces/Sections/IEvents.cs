namespace BeatmapParser;

/// <summary>
///
/// </summary>
public interface IEvents
{
    /// <summary>
    /// Represents the list of events in the beatmap.
    /// </summary>
    List<IEvent> EventList { get; set; }
}