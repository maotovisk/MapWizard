namespace BeatmapParser;
/// <summary>
/// Event class.
/// </summary>
public class Event : IEvent
{
    /// <summary>
    ///  Time of the event.
    /// </summary>
    public TimeSpan? Time { get; set; }

    /// <summary>
    ///  Initializes a new instance of the <see cref="Event"/> class.
    /// </summary>
    /// <param name="time"></param>
    public Event(TimeSpan time)
    {
        Time = time;
    }
}